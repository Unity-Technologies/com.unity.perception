#if URP_PRESENT
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Perception;
using UnityEngine.Perception.Sensors;

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
}
class SemanticSegmentationUrpPass : ScriptableRenderPass
{
    SemanticSegmentationCrossPipelinePass m_SemanticSegmentationCrossPipelinePass;

    public SemanticSegmentationUrpPass(Camera camera, RenderTexture targetTexture, LabelingConfiguration labelingConfiguration)
    {
        m_SemanticSegmentationCrossPipelinePass = new SemanticSegmentationCrossPipelinePass(camera, labelingConfiguration);
        ConfigureTarget(targetTexture, targetTexture.depthBuffer);
        m_SemanticSegmentationCrossPipelinePass.Setup();
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var commandBuffer = CommandBufferPool.Get(nameof(SemanticSegmentationUrpPass));
        m_SemanticSegmentationCrossPipelinePass.Execute(context, commandBuffer, renderingData.cameraData.camera, renderingData.cullResults);
        CommandBufferPool.Release(commandBuffer);
    }
}

public class GroundTruthRendererFeature : ScriptableRendererFeature
{
    public override void Create()
    {
    }

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

        renderer.EnqueuePass(perceptionCamera.instanceSegmentationUrpPass);
        renderer.EnqueuePass(perceptionCamera.semanticSegmentationUrpPass);
    }
}
#endif
