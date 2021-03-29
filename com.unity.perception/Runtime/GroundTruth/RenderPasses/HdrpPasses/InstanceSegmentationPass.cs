#if HDRP_PRESENT
using System;
using JetBrains.Annotations;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A CustomPass for creating object instance segmentation images. GameObjects containing Labeling components
    /// are assigned unique IDs, which are rendered into the target texture.
    /// </summary>
    public class InstanceSegmentationPass : CustomPass
    {
        InstanceSegmentationCrossPipelinePass m_InstanceSegmentationCrossPipelinePass;

        public RenderTexture targetTexture;
        public Camera targetCamera;

        [UsedImplicitly]
        public InstanceSegmentationPass() {}

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
            CoreUtils.SetRenderTarget(cmd, targetTexture, ClearFlag.All);
            m_InstanceSegmentationCrossPipelinePass.Execute(renderContext, cmd, hdCamera.camera, cullingResult);
        }

        public void EnsureInit()
        {
            if (m_InstanceSegmentationCrossPipelinePass == null)
            {
                m_InstanceSegmentationCrossPipelinePass = new InstanceSegmentationCrossPipelinePass(targetCamera);
                m_InstanceSegmentationCrossPipelinePass.Setup();
            }
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            base.Setup(renderContext, cmd);
            Debug.Assert(m_InstanceSegmentationCrossPipelinePass != null, "InstanceSegmentationPass.EnsureInit() should be called before the first camera render to get proper object labels in the first frame");
            EnsureInit();
        }
    }
}
#endif
