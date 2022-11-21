using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors
{
#if PERCEPTION_EXPERIMENTAL
    /// <summary>
    /// A <see cref="CameraSensor"/> that can generate fisheye projections with field of views up to 360 degrees.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth.Internal")]
    public class CircularFisheyeCameraSensor : CubemapCameraSensor
    {
        static readonly int k_RenderCorners = Shader.PropertyToID("_RenderCorners");
        static readonly int k_CubemapTex = Shader.PropertyToID("_CubemapTex");
        static readonly int k_FieldOfView = Shader.PropertyToID("_FieldOfView");

        static Material s_CircularFisheyeMaterial;

        /// <summary>
        /// The field of view of the fisheye sensor in degrees.
        /// </summary>
        [Range(1, 360)] public float fieldOfView = 360f;

        /// <summary>
        /// Enables the corners of the fisheye image that exceed the sensor's designated field of view
        /// to be rendered as long as the extended fov is below the 360 degree fov limit.
        /// </summary>
        [Tooltip("Enables the corners of the fisheye image that exceed the sensor's designated field of view to be " +
            "rendered as long as the extended fov is below the 360 degree fov limit.")]
        public bool renderCorners = true;

        /// <inheritdoc/>
        public override CameraSensorIntrinsics intrinsics => new()
        {
            projection = "fisheye",
            matrix = float3x3.identity
        };

        /// <inheritdoc/>
        protected override void Setup()
        {
            base.Setup();
            if (s_CircularFisheyeMaterial == null)
                s_CircularFisheyeMaterial = new(RenderUtilities.LoadPrewarmedShader("Perception/CircularFisheyeProjection"));
        }

        /// <inheritdoc/>
        protected override void ProjectFisheyeFromCubemap(
            CommandBuffer cmd, RenderTexture cubemap, RenderTargetIdentifier output)
        {
            cmd.SetGlobalInteger(k_RenderCorners, renderCorners ? 1 : 0);
            cmd.SetGlobalFloat(k_FieldOfView, fieldOfView);
            cmd.SetGlobalTexture(k_CubemapTex, cubemap);
            cmd.Blit(cubemap, output, s_CircularFisheyeMaterial);
        }
    }
#endif
}
