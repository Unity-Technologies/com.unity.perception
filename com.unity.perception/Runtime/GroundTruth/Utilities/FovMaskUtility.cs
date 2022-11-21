using Unity.Mathematics;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Helper utility that uses compute shaders to exclude pixels that are inside of the camera's field of view.
    /// </summary>
    static class FovMaskUtility
    {
        static readonly int k_PropPixelWeights = Shader.PropertyToID("pixelWeights");
        static readonly int k_PropMaskedPixelWeights = Shader.PropertyToID("maskedPixelWeights");
        static readonly int k_PropWidth = Shader.PropertyToID("width");
        static readonly int k_PropVerticalFov = Shader.PropertyToID("verticalFov");
        static readonly int k_PropHorizontalFov = Shader.PropertyToID("horizontalFov");
        static readonly int k_PropVerticalFovOffset = Shader.PropertyToID("verticalFovOffset");
        static readonly int k_PropHorizontalFovOffset = Shader.PropertyToID("horizontalFovOffset");

        static ComputeShader s_Shader;
        static int3 s_ThreadGroupSize;

        static FovMaskUtility()
        {
            s_Shader = ComputeUtilities.LoadShader("FieldOfViewMask");
            s_ThreadGroupSize = ComputeUtilities.GetKernelThreadGroupSizes(s_Shader, 0);
        }

        static readonly Vector2[] k_FovOffsets =
        {
            new Vector2(0f, 0f),
            new Vector2(-Mathf.PI / 2f, 0f),
            new Vector2(Mathf.PI / 2f, 0f),
            new Vector2(0f, -Mathf.PI / 2f),
            new Vector2(0f, Mathf.PI / 2f),
            new Vector2(Mathf.PI, 0f),
        };

        /// <summary>
        /// Masks a pixel weights cubemap texture by the field of view of a perspective projection, depending on the
        /// cubemap direction (forward, back, left, right, up, down) of said pixel weights texture.
        ///
        /// The purpose of this utility is to zero out the weights of each pixel visible within a camera's
        /// field of view. This utility enables the occlusion labeler to only count the contribution of object pixels
        /// that are outside of the camera's field of view.
        ///
        /// For example, if the camera in question has a 90 degrees perspective projection and an aspect ratio of
        /// 1:1 (square), executing this utility on all the faces of a pixel weights cubemap will result in only the
        /// front cubemap face having all its pixel weights cleared to zero. This is because only pixels within the
        /// front face of the cubemap would be visible by this camera's perspective projection.
        /// </summary>
        /// <param name="cmd">The CommandBuffer for which to enqueue this operation.</param>
        /// <param name="input">The input pixel weights RenderTexture to mask.</param>
        /// <param name="output">The RenderTexture the masked output will be written to.</param>
        /// <param name="fov">The vertical field of view of the perspective projection.</param>
        /// <param name="aspect">The aspect ratio (width/height) of the perspective projection.</param>
        /// <param name="cubemapFaceIndex">The cubemap face index to operate on.</param>
        public static void MaskByFov(
            CommandBuffer cmd, RenderTexture input, RenderTexture output,
            float fov, float aspect, int cubemapFaceIndex)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Mask By FOV")))
            {
                var sqrWidth = input.width;
                var verticalFov = fov * Mathf.Deg2Rad;
                var horizontalFov = Camera.VerticalToHorizontalFieldOfView(fov, aspect) * Mathf.Deg2Rad;
                var offsets = k_FovOffsets[cubemapFaceIndex];

                cmd.SetComputeTextureParam(s_Shader, 0, k_PropPixelWeights, input);
                cmd.SetComputeTextureParam(s_Shader, 0, k_PropMaskedPixelWeights, output);
                cmd.SetComputeIntParam(s_Shader, k_PropWidth, sqrWidth);
                cmd.SetComputeFloatParam(s_Shader, k_PropVerticalFov, verticalFov);
                cmd.SetComputeFloatParam(s_Shader, k_PropHorizontalFov, horizontalFov);
                cmd.SetComputeFloatParam(s_Shader, k_PropHorizontalFovOffset, offsets.x);
                cmd.SetComputeFloatParam(s_Shader, k_PropVerticalFovOffset, offsets.y);

                var threadGroupsX = ComputeUtilities.ThreadGroupsCount(sqrWidth, s_ThreadGroupSize.x);
                var threadGroupsY = ComputeUtilities.ThreadGroupsCount(sqrWidth, s_ThreadGroupSize.y);
                cmd.DispatchCompute(s_Shader, 0, threadGroupsX, threadGroupsY, 1);
            }
        }
    }
}
