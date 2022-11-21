#if HDRP_PRESENT
using System;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.VolumeEffects
{
    [Serializable]
    [MovedFrom("UnityEngine.Perception.Internal.Effects")]
    public class CameraTypeEffect : VolumeEffect
    {
        public override string displayName => "Camera Type";

        public CategoricalParameter<CameraSpecification> cameraSpecifications = new CategoricalParameter<CameraSpecification>();
        public Camera targetCamera;

        public override void SetActive(bool active) {}

        public override void RandomizeEffect()
        {
            if (targetCamera == null || cameraSpecifications.Count <= 0)
                return;

            var spec = cameraSpecifications.Sample();
            targetCamera.usePhysicalProperties = true;
            targetCamera.focalLength = spec.focalLength;
            targetCamera.sensorSize = spec.sensorSize;
            targetCamera.lensShift = spec.lensShift;
            targetCamera.gateFit = spec.gateFitMode;
        }

        public override void Dispose() {}
    }
}
#endif
