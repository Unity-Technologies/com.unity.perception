﻿using System;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

#if HDRP_PRESENT
    using UnityEngine.Rendering.HighDefinition;
#else
    using UnityEngine.Rendering.Universal;
#endif

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Custom Pass which renders labeled images where each object labeled with a Labeling component is drawn with the
    /// value specified by the given LabelingConfiguration.
    /// </summary>
    public class LensDistortionCrossPipelinePass : GroundTruthCrossPipelinePass
    {
        const string k_ShaderName = "Perception/LensDistortion";

        //static int s_LastFrameExecuted = -1;

        //Serialize the shader so that the shader asset is included in player builds when the SemanticSegmentationPass is used.
        //Currently commented out and shaders moved to Resources folder due to serialization crashes when it is enabled.
        //See https://fogbugz.unity3d.com/f/cases/1187378/
        //[SerializeField]

        // Lens Distortion Shader
        private Shader m_LensDistortionShader;
        private Material m_LensDistortionMaterial;

        private bool fInitialized = false;

        public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
        public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");

        public LensDistortion m_lensDistortion;
        public float? lensDistortionOverride = null;        // Largely for testing, but could be useful otherwise

        public RenderTexture m_TargetTexture;
        private RenderTexture m_distortedTexture;

        //private LensDistortion m_LensDistortion;

        public LensDistortionCrossPipelinePass(Camera targetCamera, RenderTexture targetTexture)
            : base(targetCamera)
        {
            m_TargetTexture = targetTexture;
        }


        public override void Setup()
        {
            base.Setup();
            m_LensDistortionShader = Shader.Find(k_ShaderName);

            var shaderVariantCollection = new ShaderVariantCollection();

            if (shaderVariantCollection != null)
                shaderVariantCollection.Add(new ShaderVariantCollection.ShaderVariant(m_LensDistortionShader, PassType.ScriptableRenderPipeline));

            m_LensDistortionMaterial = new Material(m_LensDistortionShader);

            if(shaderVariantCollection != null)
                shaderVariantCollection.WarmUp();

            // Set up a new texture
            if (m_distortedTexture == null || m_distortedTexture.width != Screen.width || m_distortedTexture.height != Screen.height) {

                if (m_distortedTexture != null)
                    m_distortedTexture.Release();

                m_distortedTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                m_distortedTexture.enableRandomWrite = true;
                m_distortedTexture.filterMode = FilterMode.Point;
                m_distortedTexture.Create();
            }

            // Grab the lens distortion
#if HDRP_PRESENT
            // Grab the Lens Distortion from Perception Camera stack
            var hdCamera = HDCamera.GetOrCreate(targetCamera);
            var stack = hdCamera.volumeStack;
            m_lensDistortion = stack.GetComponent<LensDistortion>();
#else
            var stack = VolumeManager.instance.stack;
            m_lensDistortion = stack.GetComponent<LensDistortion>();
#endif

            fInitialized = true;
        }

        protected override void ExecutePass(ScriptableRenderContext renderContext, CommandBuffer cmd, Camera camera, CullingResults cullingResult)
        {
            // Review: This has been removed since we may apply this pass to various stages, just want to confirm I'm
            // not missing anything before removing it
            //if (s_LastFrameExecuted == Time.frameCount)
            //    Debug.LogError("Lens Distortion executed twice in the same frame, this may lead to undesirable results.");
            //s_LastFrameExecuted = Time.frameCount;

            SetLensDistortionShaderParameters();

                // Blitmayhem
            cmd.Blit(m_TargetTexture, m_distortedTexture, m_LensDistortionMaterial);
            cmd.Blit(m_distortedTexture, m_TargetTexture);

            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public void SetLensDistortionShaderParameters()
        {
            if (fInitialized == false)
                return;

            // This code is lifted from the SetupLensDistortion() function in
            // https://github.com/Unity-Technologies/Graphics/blob/257b08bba6c11de0f894e42e811124247a522d3c/com.unity.render-pipelines.universal/Runtime/Passes/PostProcessPass.cs
            // This is in UnityEngine.Rendering.Universal.Internal.PostProcessPass::SetupLensDistortion so it's
            // unclear how to re-use this code

            float intensity = 0.5f;
            float scale = 1.0f;
            var center = new Vector2(0.0f, 0.0f);
            var mult = new Vector2(1.0f, 1.0f);

            if (lensDistortionOverride.HasValue)
            {
                intensity = lensDistortionOverride.Value;
            }
            else if (m_lensDistortion != null)
            {
                intensity = m_lensDistortion.intensity.value;
                center = m_lensDistortion.center.value * 2f - Vector2.one;
                mult.x = Mathf.Max(m_lensDistortion.xMultiplier.value, 1e-4f);
                mult.y = Mathf.Max(m_lensDistortion.yMultiplier.value, 1e-4f);
                scale = 1.0f / m_lensDistortion.scale.value;
            }

            float amount = 1.6f * Mathf.Max(Mathf.Abs(intensity * 100.0f), 1.0f);
            float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
            float sigma = 2.0f * Mathf.Tan(theta * 0.5f);

            var p1 = new Vector4(
                center.x,
                center.y,
                mult.x,
                mult.y
            );

            var p2 = new Vector4(
                intensity >= 0f ? theta : 1f / theta,
                sigma,
                scale,
                intensity * 100.0f
            );

            // Set Shader Constants
            m_LensDistortionMaterial.SetVector(_Distortion_Params1, p1);
            m_LensDistortionMaterial.SetVector(_Distortion_Params2, p2);
        }

        public override void SetupMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId)
        {
            SetLensDistortionShaderParameters();
        }
    }
}
