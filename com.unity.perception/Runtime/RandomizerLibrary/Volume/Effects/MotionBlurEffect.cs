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
    public class MotionBlurEffect : VolumeEffect
    {
        public override string displayName => "Motion Blur";

        public FloatParameter intensity = new FloatParameter { value = new UniformSampler(0f, 1f) };
        public FloatParameter minimumVelocity = new FloatParameter { value = new UniformSampler(0f, 1f) };
        public FloatParameter maximumVelocity = new FloatParameter { value = new UniformSampler(0f, 1f) };

        MotionBlur m_MotionBlur;
        public MotionBlur motionBlur => m_MotionBlur = m_MotionBlur ? m_MotionBlur : GetVolumeComponent(ref m_MotionBlur);

        public override void SetActive(bool active)
        {
            motionBlur.active = active;
        }

        public override void RandomizeEffect()
        {
            motionBlur.intensity.value = intensity.Sample();
            motionBlur.minimumVelocity.value = minimumVelocity.Sample();
            motionBlur.maximumVelocity.value = maximumVelocity.Sample();
        }

        public override void Dispose() {}
    }
}
#endif
