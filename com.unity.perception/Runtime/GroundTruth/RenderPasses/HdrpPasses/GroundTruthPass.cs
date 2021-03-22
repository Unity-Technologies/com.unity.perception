#if HDRP_PRESENT
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Perception.GroundTruth
{
    abstract class GroundTruthPass : CustomPass, IGroundTruthGenerator
    {
        public Camera targetCamera;

        bool m_IsActivated;
        public abstract void SetupMaterialProperties(
            MaterialPropertyBlock mpb, Renderer meshRenderer, Labeling labeling, uint instanceId);

        public abstract void ClearMaterialProperties(
            MaterialPropertyBlock mpb, Renderer meshRenderer, Labeling labeling, uint instanceId);

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

            targetColorBuffer = TargetBuffer.Custom;
            targetDepthBuffer = TargetBuffer.Custom;
        }

        //overrides obsolete member in HDRP on 2020.1+. Re-address when removing 2019.4 support or the API is dropped
#if HDRP_9_OR_NEWER
        protected override void Execute(CustomPassContext ctx)
        {
            ScriptableRenderContext renderContext = ctx.renderContext;
            var cmd = ctx.cmd;
            var hdCamera = ctx.hdCamera;
            var cullingResult = ctx.cullingResults;
#else
        protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
        {
#endif
            // CustomPasses are executed for each camera. We only want to run for the target camera
            if (hdCamera.camera != targetCamera)
                return;

            ExecutePass(renderContext, cmd, hdCamera, cullingResult);
        }

        protected abstract void ExecutePass(
            ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult);

        protected void EnsureActivated()
        {
            if (!m_IsActivated)
            {
                LabelManager.singleton.Activate(this);
                m_IsActivated = true;
            }
        }

        protected override void Cleanup()
        {
            LabelManager.singleton.Deactivate(this);
        }
    }
}
#endif
