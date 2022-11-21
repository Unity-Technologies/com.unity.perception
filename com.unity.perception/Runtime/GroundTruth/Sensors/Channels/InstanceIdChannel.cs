using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;
using RendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;

namespace UnityEngine.Perception.GroundTruth.Sensors.Channels
{
    /// <summary>
    /// A <see cref="CameraChannel{T}"/> that outputs the instance index of the labeled object
    /// captured by each pixel in an <see cref="CameraSensor">CameraSensor's</see> output texture.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class InstanceIdChannel : CameraChannel<uint>, IPostProcessChannel
    {
        static readonly int k_AlphaThreshold = Shader.PropertyToID("_AlphaThreshold");
        static readonly int k_FloatTexture = Shader.PropertyToID("floatTexture");
        static readonly int k_UIntTexture = Shader.PropertyToID("uintTexture");
        static Material s_InstanceIdIndexMaterial = new(RenderUtilities.LoadPrewarmedShader("Perception/InstanceIdIndex"));
        static ComputeShader s_FloatToUIntShader = ComputeUtilities.LoadShader("InstanceIdFloatToUInt");

        /// <inheritdoc/>
        public override Color clearColor => Color.clear;

        /// <inheritdoc/>
        public RenderTexture preprocessTexture { get; set; }

        /// <inheritdoc/>
        public RenderTexture CreatePreprocessTexture(int width, int height)
        {
            var instanceIdIndicesTexture = new RenderTexture(
                width, height, 32, GraphicsFormat.R32_SFloat)
            {
                name = "Instance Id Float Channel",
                enableRandomWrite = true,
                filterMode = FilterMode.Point
            };
            instanceIdIndicesTexture.Create();

            return instanceIdIndicesTexture;
        }

        /// <inheritdoc/>
        public override RenderTexture CreateOutputTexture(int width, int height)
        {
            LabelManager.singleton.Activate<SegmentationGenerator>();

            var instanceIdIndicesTexture = new RenderTexture(
                width, height, 32, GraphicsFormat.R32_UInt)
            {
                name = "Instance Id UInt Channel",
                enableRandomWrite = true,
                filterMode = FilterMode.Point
            };
            instanceIdIndicesTexture.Create();

            return instanceIdIndicesTexture;
        }

        /// <inheritdoc/>
        public override void Execute(CameraChannelInputs inputs, RenderTexture renderTarget)
        {
            var rendererListDesc = new RendererListDesc(
                RenderUtilities.shaderPassNames, inputs.cullingResults, inputs.camera)
            {
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = SortingCriteria.BackToFront,
                excludeObjectMotionVectors = false,
                overrideMaterial = s_InstanceIdIndexMaterial,
                layerMask = perceptionCamera.layerMask
            };

            var list = inputs.ctx.CreateRendererList(rendererListDesc);

            var cmd = inputs.cmd;
            cmd.SetRenderTarget(renderTarget);
            cmd.ClearRenderTarget(true, true, clearColor);
            cmd.SetGlobalFloat(k_AlphaThreshold, perceptionCamera.alphaThreshold);
            inputs.cmd.DrawRendererList(list);
        }

        /// <inheritdoc/>
        public void PostProcessChannelOutput(
            ScriptableRenderContext ctx, CommandBuffer cmd, RenderTexture input, RenderTexture output)
        {
            var threadGroupsX = ComputeUtilities.ThreadGroupsCount(input.width, 16);
            var threadGroupsY = ComputeUtilities.ThreadGroupsCount(input.height, 16);

            cmd.SetRenderTarget(output);
            cmd.ClearRenderTarget(true, true, clearColor);

            cmd.SetComputeTextureParam(s_FloatToUIntShader, 0, k_FloatTexture, input);
            cmd.SetComputeTextureParam(s_FloatToUIntShader, 0, k_UIntTexture, output);
            cmd.DispatchCompute(s_FloatToUIntShader, 0, threadGroupsX, threadGroupsY, 1);
        }
    }
}
