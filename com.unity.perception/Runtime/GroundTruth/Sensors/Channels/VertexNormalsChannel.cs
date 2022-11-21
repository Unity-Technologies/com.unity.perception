using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors.Channels
{
    /// <summary>
    /// A <see cref="CameraChannel{T}"/> that outputs the vertex normal of the surface
    /// captured by each pixel in the <see cref="CameraSensor">CameraSensor's</see> output texture.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class VertexNormalsChannel : CameraChannel<float4>, IPostProcessChannel
    {
        static readonly int k_WorldToSensorMatrix = Shader.PropertyToID("worldToSensorMatrix");
        static readonly int k_WorldNormalsTexture = Shader.PropertyToID("worldNormalsTexture");
        static readonly int k_SensorNormalsTexture = Shader.PropertyToID("sensorNormalsTexture");
        static Material s_VertexNormalsMaterial = new(RenderUtilities.LoadPrewarmedShader("Perception/VertexNormals"));
        static ComputeShader s_WorldToSensorNormalsShader = ComputeUtilities.LoadShader("WorldToSensorNormals");

        /// <inheritdoc/>
        public override Color clearColor => Color.black;

        /// <inheritdoc/>
        public RenderTexture preprocessTexture { get; set; }

        /// <inheritdoc/>
        public RenderTexture CreatePreprocessTexture(int width, int height)
        {
            var texture = new RenderTexture(
                width, height, 32, GraphicsFormat.R32G32B32A32_SFloat)
            {
                name = "Vertex Normals Channel",
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear
            };
            texture.Create();
            return texture;
        }

        /// <inheritdoc/>
        public override RenderTexture CreateOutputTexture(int width, int height)
        {
            var texture = new RenderTexture(
                width, height, 32, GraphicsFormat.R32G32B32A32_SFloat)
            {
                name = "Vertex Normals Channel",
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear
            };
            texture.Create();
            return texture;
        }

        /// <inheritdoc/>
        public override void Execute(CameraChannelInputs inputs, RenderTexture renderTarget)
        {
            var rendererListDesc = RenderUtilities.CreateRendererListDesc(
                inputs.camera, inputs.cullingResults, s_VertexNormalsMaterial, 0, perceptionCamera.layerMask);
            rendererListDesc.sortingCriteria = SortingCriteria.BackToFront;
            var list = inputs.ctx.CreateRendererList(rendererListDesc);

            inputs.cmd.SetRenderTarget(renderTarget);
            inputs.cmd.ClearRenderTarget(true, true, clearColor);
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

            cmd.SetComputeMatrixParam(s_WorldToSensorNormalsShader, k_WorldToSensorMatrix,
                Matrix4x4.Rotate(perceptionCamera.transform.rotation).inverse);
            cmd.SetComputeTextureParam(s_WorldToSensorNormalsShader, 0, k_WorldNormalsTexture, input);
            cmd.SetComputeTextureParam(s_WorldToSensorNormalsShader, 0, k_SensorNormalsTexture, output);
            cmd.DispatchCompute(s_WorldToSensorNormalsShader, 0, threadGroupsX, threadGroupsY, 1);
        }
    }
}
