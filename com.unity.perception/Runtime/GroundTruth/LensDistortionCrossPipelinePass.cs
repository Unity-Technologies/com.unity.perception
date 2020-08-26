using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Custom Pass which renders labeled images where each object labeled with a Labeling component is drawn with the
    /// value specified by the given LabelingConfiguration.
    /// </summary>
    class LensDistortionCrossPipelinePass : GroundTruthCrossPipelinePass
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

        public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
        public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");

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
        }

        protected override void ExecutePass(ScriptableRenderContext renderContext, CommandBuffer cmd, Camera camera, CullingResults cullingResult)
        {
            // Review: This has been removed since we may apply this pass to various stages, just want to confirm I'm
            // not missing anything before removing it
            //if (s_LastFrameExecuted == Time.frameCount)
            //    Debug.LogError("Lens Distortion executed twice in the same frame, this may lead to undesirable results.");
            //s_LastFrameExecuted = Time.frameCount;

            var stack = VolumeManager.instance.stack;
            var lensDistortion = stack.GetComponent<LensDistortion>();
            Debug.Log("lens distortion intensity: " + lensDistortion.intensity.value);

            // Blitmayhem
            cmd.Blit(m_TargetTexture, m_distortedTexture, m_LensDistortionMaterial);
            cmd.Blit(m_distortedTexture, m_TargetTexture);
        }

        public override void SetupMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId)
        {
            // Grab the Lens Distortion from Perception Camera stack
            var hdCamera = HDCamera.GetOrCreate(targetCamera);
            var stack = hdCamera.volumeStack;
            var lensDistortion = stack.GetComponent<LensDistortion>();
            Debug.Log("lens distortion intensity: " + lensDistortion.intensity.value);

            // This code is lifted from the SetupLensDistortion() function in
            // https://github.com/Unity-Technologies/Graphics/blob/257b08bba6c11de0f894e42e811124247a522d3c/com.unity.render-pipelines.universal/Runtime/Passes/PostProcessPass.cs
            // This is in UnityEngine.Rendering.Universal.Internal.PostProcessPass::SetupLensDistortion so it's
            // unclear how to re-use this code

            float amount = 1.6f * Mathf.Max(Mathf.Abs(lensDistortion.intensity.value * 100.0f), 1.0f);
            float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
            float sigma = 2.0f * Mathf.Tan(theta * 0.5f);
            var center = lensDistortion.center.value * 2f - Vector2.one;
            var p1 = new Vector4(
                center.x,
                center.y,
                Mathf.Max(lensDistortion.xMultiplier.value, 1e-4f),
                Mathf.Max(lensDistortion.yMultiplier.value, 1e-4f)
            );

            var p2 = new Vector4(
                lensDistortion.intensity.value >= 0f ? theta : 1f / theta,
                sigma,
                1.0f / lensDistortion.scale.value,
                lensDistortion.intensity.value * 100.0f
            );

            // Set Shader Constants
            m_LensDistortionMaterial.SetVector(_Distortion_Params1, p1);
            m_LensDistortionMaterial.SetVector(_Distortion_Params2, p2);

        }
    }
}
