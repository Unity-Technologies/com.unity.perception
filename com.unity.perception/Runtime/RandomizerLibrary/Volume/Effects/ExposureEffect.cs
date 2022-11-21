#if HDRP_PRESENT
using System;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting.APIUpdating;
using FloatParameter = UnityEngine.Perception.Randomization.Parameters.FloatParameter;

namespace UnityEngine.Perception.Randomization.VolumeEffects
{
    [Serializable]
    [MovedFrom("UnityEngine.Perception.Internal.Effects")]
    public class ExposureEffect : VolumeEffect
    {
        public override string displayName => "Exposure";

        public FloatParameter Compensation = new FloatParameter()
        {
            value = new UniformSampler(3f, 5f)
        };

        Exposure m_Exposure;
        public Exposure exposure => m_Exposure = m_Exposure ? m_Exposure : GetVolumeComponent(ref m_Exposure);

        public override void SetActive(bool active)
        {
            exposure.active = active;
        }

        public override void SetupEffect()
        {
            exposure.mode = new ExposureModeParameter(ExposureMode.UsePhysicalCamera);
        }

        public override void RandomizeEffect()
        {
            exposure.compensation.value = Compensation.Sample();
        }

        public override void Dispose() {}
    }
}
#endif
