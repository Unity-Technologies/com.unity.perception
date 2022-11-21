namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A set of utilities for indexing and capturing cubemap textures.
    /// </summary>
    static class CubemapUtility
    {
        /// <summary>
        /// The camera direction corresponding to each face index in a Unity cubemap texture.
        /// </summary>
        public static readonly Quaternion[] cameraDirections =
        {
            Quaternion.Euler(0, 90, 0),
            Quaternion.Euler(0, -90, 0),
            Quaternion.Euler(90, 0, 0),
            Quaternion.Euler(-90, 0, 0),
            Quaternion.identity,
            Quaternion.Euler(0, 180, 0)
        };

        /// <summary>
        /// The direction each Unity cubemap face captures.
        /// </summary>
        public static readonly string[] cameraDirectionNames =
        {
            "Right",
            "Left",
            "Down",
            "Up",
            "Forward",
            "Back"
        };

        /// <summary>
        /// Creates a new set of 6 cameras, one per cube map direction,
        /// based on the settings applied to an existing reference camera.
        /// </summary>
        /// <param name="parent">The parent transform to nest the new camera objects under.</param>
        /// <param name="referenceCamera">
        /// The camera to use as a reference for the settings to configure for each cube face camera.
        /// </param>
        /// <returns>An array off six cameras, each corresponding to </returns>
        public static Camera[] CreateCubemapCameras(Transform parent, Camera referenceCamera)
        {
            var cameras = new Camera[6];

            for (var i = 0; i < cameraDirections.Length; i++)
            {
                var cameraObj = new GameObject($"{referenceCamera.name} Cubemap: {cameraDirectionNames[i]}");
                cameraObj.transform.parent = parent;
                cameraObj.hideFlags = HideFlags.HideAndDontSave;

                var camera = cameraObj.AddComponent<Camera>();
                cameras[i] = camera;
                camera.CopyFrom(referenceCamera);
                camera.fieldOfView = 90f;
                camera.transform.localRotation = cameraDirections[i];
                camera.depth = referenceCamera.depth + 1;
            }

            return cameras;
        }
    }
}
