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
    public class LensDistortionPass : CustomPass
    {
        public RenderTexture targetTexture;
        public Camera targetCamera;

        internal LensDistortionCrossPipelinePass lensDistortionCrossPipelinePass;

        public LensDistortionPass(Camera targetCamera, RenderTexture targetTexture)
        {
            this.targetTexture = targetTexture;
            this.targetCamera = targetCamera;
            EnsureInit();
        }

        public void EnsureInit()
        {
            if (lensDistortionCrossPipelinePass == null)
            {
                lensDistortionCrossPipelinePass = new LensDistortionCrossPipelinePass(targetCamera, targetTexture);
                lensDistortionCrossPipelinePass.EnsureActivated();
            }
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            EnsureInit();
            lensDistortionCrossPipelinePass.Setup();
        }

        protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
        {
            CoreUtils.SetRenderTarget(cmd, targetTexture);
            lensDistortionCrossPipelinePass.Execute(renderContext, cmd, hdCamera.camera, cullingResult);
        }
    }
}
#endif
