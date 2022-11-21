#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Utilities
{
    /// <summary>
    /// A set of utilities for manipulating camera components.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    static class CameraUtilities
    {
        /// <summary>
        /// Creates a copy of the given camera component and adds it to a
        /// new child GameObject parented under the given camera's GameObject.
        /// </summary>
        /// <param name="camera">The camera to duplicate.</param>
        public static Camera DuplicateCamera(Camera camera)
        {
            var cameraCopyObj = new GameObject($"{camera.name} (Copy)");
            cameraCopyObj.transform.parent = camera.transform;

            var cameraCopy = cameraCopyObj.AddComponent<Camera>();
            cameraCopy.CopyFrom(camera);
#if HDRP_PRESENT
            if (camera.TryGetComponent<HDAdditionalCameraData>(out var cameraData))
            {
                var cameraDataCopy = cameraCopyObj.AddComponent<HDAdditionalCameraData>();
                cameraData.CopyTo(cameraDataCopy);
            }
#endif
            return cameraCopy;
        }

        /// <summary>
        /// Configures the given camera such that it remains enabled, but does not actually render anything.
        /// </summary>
        /// <param name="camera">The camera to convert to a passthrough camera.</param>
        public static void ConvertToPassThroughCamera(Camera camera)
        {
#if HDRP_PRESENT
            if (!camera.TryGetComponent<HDAdditionalCameraData>(out var cameraData))
                cameraData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            cameraData.fullscreenPassthrough = true;
#endif
        }

        /// <summary>
        /// Enables depth texture generation and availability on a Unity Camera.
        /// </summary>
        /// <param name="camera">The camera to enable the depth texture on.</param>
        public static void EnableDepthTexture(Camera camera)
        {
            camera.depthTextureMode = DepthTextureMode.Depth;
        }
    }
}
