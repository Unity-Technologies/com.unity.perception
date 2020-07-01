#if HDRP_PRESENT

using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Custom Pass which renders labeled images where each object with a Labeling component is drawn with the value
    /// specified by the given LabelingConfiguration.
    /// </summary>
    public class SemanticSegmentationPass : CustomPass
    {
        public RenderTexture targetTexture;
        public SemanticSegmentationLabelConfig semanticSegmentationLabelConfig;
        public Camera targetCamera;

        SemanticSegmentationCrossPipelinePass m_SemanticSegmentationCrossPipelinePass;

        public SemanticSegmentationPass(Camera targetCamera, RenderTexture targetTexture, SemanticSegmentationLabelConfig semanticSegmentationLabelConfig)
        {
            this.targetTexture = targetTexture;
            this.semanticSegmentationLabelConfig = semanticSegmentationLabelConfig;
            this.targetCamera = targetCamera;
            EnsureInit();
        }

        void EnsureInit()
        {
            if (m_SemanticSegmentationCrossPipelinePass == null)
            {
                m_SemanticSegmentationCrossPipelinePass = new SemanticSegmentationCrossPipelinePass(targetCamera, semanticSegmentationLabelConfig);
                m_SemanticSegmentationCrossPipelinePass.EnsureActivated();
            }
        }

        public SemanticSegmentationPass()
        {
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            EnsureInit();
            m_SemanticSegmentationCrossPipelinePass.Setup();
        }

        protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
        {
            CoreUtils.SetRenderTarget(cmd, targetTexture, ClearFlag.All);
            m_SemanticSegmentationCrossPipelinePass.Execute(renderContext, cmd, hdCamera.camera, cullingResult);
        }
    }
}
#endif
