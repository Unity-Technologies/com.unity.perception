using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Custom Pass which renders labeled images where each object labeled with a Labeling component is drawn with the
    /// value specified by the given LabelingConfiguration.
    /// </summary>
    class LensDistortionCrossPipelinePass : GroundTruthCrossPipelinePass
    {
        const string k_ShaderName = "Perception/LensDistortion";

        static int s_LastFrameExecuted = -1;

        //Serialize the shader so that the shader asset is included in player builds when the SemanticSegmentationPass is used.
        //Currently commented out and shaders moved to Resources folder due to serialization crashes when it is enabled.
        //See https://fogbugz.unity3d.com/f/cases/1187378/
        //[SerializeField]

        private Shader m_LensDistortionShader;
        private Material m_LensDistortionMaterial;

        public RenderTexture m_TargetTexture;
        private RenderTexture m_distortedTexture;

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
                m_distortedTexture.Create();
            }
        }

        protected override void ExecutePass(ScriptableRenderContext renderContext, CommandBuffer cmd, Camera camera, CullingResults cullingResult)
        {
            if (s_LastFrameExecuted == Time.frameCount)
                Debug.LogError("Lens Distortion executed twice in the same frame, this may lead to undesirable results.");
            s_LastFrameExecuted = Time.frameCount;

            // Blitmayhem
            cmd.Blit(m_TargetTexture, m_distortedTexture, m_LensDistortionMaterial);
            cmd.Blit(m_distortedTexture, m_TargetTexture);
        }

        public override void SetupMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId)
        {
            // Note: Problably don't need this

            /*
            var entry = new SemanticSegmentationLabelEntry();
            bool found = false;

            foreach (var l in m_LabelConfig.labelEntries)
            {
                if (labeling.labels.Contains(l.label))
                {
                    entry = l;
                    found = true;
                    break;
                }
            }

            //Set the labeling ID so that it can be accessed in ClassSemanticSegmentationPass.shader
            if (found)
                mpb.SetVector(k_LabelingId, entry.color);
            */
        }
    }
}
