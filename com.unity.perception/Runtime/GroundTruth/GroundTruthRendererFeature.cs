#if URP_PRESENT
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Perception.GroundTruth
{
    class InstanceSegmentationUrpPass : ScriptableRenderPass
    {
        InstanceSegmentationCrossPipelinePass m_InstanceSegmentationPass;

        public InstanceSegmentationUrpPass(Camera camera, RenderTexture targetTexture)
        {
            m_InstanceSegmentationPass = new InstanceSegmentationCrossPipelinePass(camera);
            ConfigureTarget(targetTexture, targetTexture.depthBuffer);
            m_InstanceSegmentationPass.Setup();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var commandBuffer = CommandBufferPool.Get(nameof(InstanceSegmentationUrpPass));
            m_InstanceSegmentationPass.Execute(context, commandBuffer, renderingData.cameraData.camera, renderingData.cullResults);
            CommandBufferPool.Release(commandBuffer);
        }

        public void Cleanup()
        {
            m_InstanceSegmentationPass.Cleanup();
        }
    }

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

    class LensDistortionUrpPass : ScriptableRenderPass
    {
        public LensDistortionCrossPipelinePass m_LensDistortionCrossPipelinePass;

        public LensDistortionUrpPass(Camera camera, RenderTexture targetTexture)
        {
            m_LensDistortionCrossPipelinePass = new LensDistortionCrossPipelinePass(camera, targetTexture);
            ConfigureTarget(targetTexture, targetTexture.depthBuffer);
            m_LensDistortionCrossPipelinePass.Setup();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var commandBuffer = CommandBufferPool.Get(nameof(SemanticSegmentationUrpPass));
            m_LensDistortionCrossPipelinePass.Execute(context, commandBuffer, renderingData.cameraData.camera, renderingData.cullResults);
            CommandBufferPool.Release(commandBuffer);
        }

        public void Cleanup()
        {
            m_LensDistortionCrossPipelinePass.Cleanup();
        }
    }

    public class GroundTruthRendererFeature : ScriptableRendererFeature
    {
        public override void Create() {}

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraObject = renderingData.cameraData.camera.gameObject;
            var perceptionCamera = cameraObject.GetComponent<PerceptionCamera>();

            if (perceptionCamera == null)
                return;

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif
            perceptionCamera.OnGroundTruthRendererFeatureRun();
            foreach (var pass in perceptionCamera.passes)
                renderer.EnqueuePass(pass);
        }
    }
}
#endif
