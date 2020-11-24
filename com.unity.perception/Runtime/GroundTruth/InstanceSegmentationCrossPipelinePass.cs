using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    class InstanceSegmentationCrossPipelinePass : GroundTruthCrossPipelinePass
    {
        static readonly int k_SegmentationIdProperty = Shader.PropertyToID("_SegmentationId");
        const string k_SegmentationPassShaderName = "Perception/InstanceSegmentation";

        static ProfilerMarker s_ExecuteMarker = new ProfilerMarker("SegmentationPass_Execute");

        /// <summary>
        /// The LayerMask to apply when rendering objects.
        /// </summary>
        public LayerMask layerMask = -1;

        Shader m_SegmentationShader;
        Material m_OverrideMaterial;

        /// <summary>
        /// Create a new <see cref="InstanceSegmentationCrossPipelinePass"/> referencing the given
        /// </summary>
        /// <param name="targetCamera"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public InstanceSegmentationCrossPipelinePass(Camera targetCamera)
            : base(targetCamera)
        {
            if (targetCamera == null)
                throw new ArgumentNullException(nameof(targetCamera));

            //Activating in the constructor allows us to get correct labeling in the first frame.
            EnsureActivated();
        }

        public override void Setup()
        {
            base.Setup();
            m_SegmentationShader = Shader.Find(k_SegmentationPassShaderName);
            var shaderVariantCollection = new ShaderVariantCollection();
            shaderVariantCollection.Add(new ShaderVariantCollection.ShaderVariant(m_SegmentationShader, PassType.ScriptableRenderPipeline));
            shaderVariantCollection.WarmUp();

            m_OverrideMaterial = new Material(m_SegmentationShader);
        }

        //Render all objects to our target RenderTexture using `overrideMaterial` to use our shader
        protected override void ExecutePass(ScriptableRenderContext renderContext, CommandBuffer cmd, Camera camera, CullingResults cullingResult)
        {
            using (s_ExecuteMarker.Auto())
            {
                cmd.ClearRenderTarget(true, true, Color.black);
                var result = CreateRendererListDesc(camera, cullingResult, "FirstPass", 0, m_OverrideMaterial, layerMask);

                DrawRendererList(renderContext, cmd, RendererList.Create(result));
            }
        }

        public override void SetupMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId)
        {
            var found = InstanceIdToColorMapping.TryGetColorFromInstanceId(instanceId, out var color);

            if (!found)
            {
                Debug.LogError($"Could not get a unique color for {instanceId}");
            }

            mpb.SetVector(k_SegmentationIdProperty, (Color)color);
    #if PERCEPTION_DEBUG
            Debug.Log($"Assigning id. Frame {Time.frameCount} id {id}");
    #endif
        }
    }
}
