#if HDRP_PRESENT

using System;
using Unity.Entities;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Perception.Sensors
{
    public abstract class GroundTruthPass : CustomPass, IGroundTruthGenerator
    {
        public Camera targetCamera;

        bool m_IsActivated;
        public abstract void SetupMaterialProperties(MaterialPropertyBlock mpb, MeshRenderer meshRenderer, Labeling labeling, uint instanceId);

        protected GroundTruthPass(Camera targetCamera)
        {
            this.targetCamera = targetCamera;
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (targetCamera == null)
                throw new InvalidOperationException("targetCamera may not be null");

            // If we are forced to activate here we will get zeroes in the first frame.
            EnsureActivated();

            this.targetColorBuffer = TargetBuffer.Custom;
            this.targetDepthBuffer = TargetBuffer.Custom;
        }

        protected sealed override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
        {
            // CustomPasses are executed for each camera. We only want to run for the target camera
            if (hdCamera.camera != targetCamera)
                return;

            ExecutePass(renderContext, cmd, hdCamera, cullingResult);
        }

        protected abstract void ExecutePass(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult);

        protected void EnsureActivated()
        {
            if (!m_IsActivated)
            {
                var labelSetupSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystem<GroundTruthLabelSetupSystem>();
                labelSetupSystem?.Activate(this);
                m_IsActivated = true;
            }
        }

        protected override void Cleanup()
        {
            var labelSetupSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystem<GroundTruthLabelSetupSystem>();
            labelSetupSystem?.Deactivate(this);
        }


        protected RendererListDesc CreateRendererListDesc(HDCamera hdCamera, CullingResults cullingResult, string overrideMaterialPassName, int overrideMaterialPassIndex, Material overrideMaterial, LayerMask layerMask)
        {
            var shaderPasses = new[]
            {
                new ShaderTagId("Forward"), // HD Lit shader
                new ShaderTagId("ForwardOnly"), // HD Unlit shader
                new ShaderTagId("SRPDefaultUnlit"), // Cross SRP Unlit shader
                new ShaderTagId(overrideMaterialPassName), // The override material shader
            };

            var stateBlock = new RenderStateBlock(0)
            {
                depthState = new DepthState(true, CompareFunction.LessEqual),
            };

            PerObjectData renderConfig = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask) ? HDUtils.k_RendererConfigurationBakedLightingWithShadowMask : HDUtils.k_RendererConfigurationBakedLighting;

            var result = new RendererListDesc(shaderPasses, cullingResult, hdCamera.camera)
            {
                rendererConfiguration = renderConfig,
                renderQueueRange = GetRenderQueueRange(RenderQueueType.All),
                sortingCriteria = SortingCriteria.CommonOpaque,
                excludeObjectMotionVectors = false,
                overrideMaterial = overrideMaterial,
                overrideMaterialPassIndex = overrideMaterialPassIndex,
                stateBlock = stateBlock,
                layerMask = layerMask,
            };
            return result;
        }
    }
}
#endif
