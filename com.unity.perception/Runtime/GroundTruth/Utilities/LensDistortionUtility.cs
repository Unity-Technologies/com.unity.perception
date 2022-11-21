#if HDRP_PRESENT
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Perception.GroundTruth.Utilities
{
    /// <summary>
    /// A utility for applying Unity's SRP lens distortion volume effect to an arbitrary texture.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    static class LensDistortionUtility
    {
        static Material s_LensDistortionMaterial = new(RenderUtilities.LoadPrewarmedShader("Perception/LensDistortion"));

        static readonly int k_DistortionParams1Id = Shader.PropertyToID("_Distortion_Params1");
        static readonly int k_DistortionParams2Id = Shader.PropertyToID("_Distortion_Params2");
        static readonly int k_TempLensDistortionRT = Shader.PropertyToID("TempLensDistortionRT");

        /// <summary>
        /// Applies Unity's SRP lens distortion volume effect to an arbitrary texture.
        /// </summary>
        /// <param name="cmd">The CommandBuffer for which to enqueue this operation.</param>
        /// <param name="targetTexture">The texture to apply lens distortion to.</param>
        /// <param name="camera">The Unity Camera component for which to obtain the lens distortion settings from.</param>
        public static void ApplyLensDistortion(CommandBuffer cmd, RenderTexture targetTexture, Camera camera)
        {
#if HDRP_PRESENT
            var hdCamera = HDCamera.GetOrCreate(camera);
            var stack = hdCamera.volumeStack;
            var lensDistortion = stack.GetComponent<LensDistortion>();
#endif

            if (!SetLensDistortionShaderParameters(cmd, camera, lensDistortion))
                return;

            // Allocate and clear a temporary render texture.
            cmd.GetTemporaryRT(
                k_TempLensDistortionRT, targetTexture.width, targetTexture.height, targetTexture.depth,
                targetTexture.filterMode, targetTexture.graphicsFormat);

            // Apply the lens distortion to the target texture, saving the result in the temporary texture.
            var passIndex = targetTexture.graphicsFormat == GraphicsFormat.R32_UInt ? 1 : 0;
            cmd.Blit(targetTexture, k_TempLensDistortionRT, s_LensDistortionMaterial, passIndex);

            // Copy the result stored in the temporary texture back to the target texture.
            cmd.CopyTexture(k_TempLensDistortionRT, targetTexture);
            cmd.ReleaseTemporaryRT(k_TempLensDistortionRT);
        }

        static bool SetLensDistortionShaderParameters(
            CommandBuffer cmd, Camera targetCamera, LensDistortion lensDistortion)
        {
            if (lensDistortion == null)
                return false;

            if (lensDistortion.intensity.value == 0.0f)
                return false;

            var intensity = lensDistortion.intensity.value;
            var center = lensDistortion.center.value * 2f - Vector2.one;
            var mult = new Vector2
            {
                x = Mathf.Max(lensDistortion.xMultiplier.value, 1e-4f),
                y = Mathf.Max(lensDistortion.yMultiplier.value, 1e-4f)
            };
            var scale = 1.0f / lensDistortion.scale.value;

            var amount = 1.6f * Mathf.Max(Mathf.Abs(intensity * 100.0f), 1.0f);
            var theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
            var sigma = 2.0f * Mathf.Tan(theta * 0.5f);

            var p1 = new Vector4(
                center.x,
                center.y,
                mult.x,
                mult.y
            );

            var p2 = new Vector4(
                intensity >= 0f ? theta : 1f / theta,
                sigma,
                scale,
                intensity * 100.0f
            );

            // Set shader constants.
            cmd.SetGlobalVector(k_DistortionParams1Id, p1);
            cmd.SetGlobalVector(k_DistortionParams2Id, p2);

            return true;
        }
    }
}
#endif
