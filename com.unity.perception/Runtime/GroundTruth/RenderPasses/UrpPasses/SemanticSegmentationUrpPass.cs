#if URP_PRESENT
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Perception.GroundTruth
{
    class SemanticSegmentationUrpPass : ScriptableRenderPass
    {
        public SemanticSegmentationCrossPipelinePass m_SemanticSegmentationCrossPipelinePass;

        public SemanticSegmentationUrpPass(Camera camera, RenderTexture targetTexture, SemanticSegmentationLabelConfig labelConfig)
        {
            m_SemanticSegmentationCrossPipelinePass = new SemanticSegmentationCrossPipelinePass(camera, labelConfig);
            ConfigureTarget(targetTexture, targetTexture.depthBuffer);
            m_SemanticSegmentationCrossPipelinePass.Setup();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var commandBuffer = CommandBufferPool.Get(nameof(SemanticSegmentationUrpPass));
            m_SemanticSegmentationCrossPipelinePass.Execute(context, commandBuffer, renderingData.cameraData.camera, renderingData.cullResults);
            CommandBufferPool.Release(commandBuffer);
        }

        public void Cleanup()
        {
            m_SemanticSegmentationCrossPipelinePass.Cleanup();
        }
    }
}
#endif
