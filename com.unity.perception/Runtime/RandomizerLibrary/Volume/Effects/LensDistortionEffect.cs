#if HDRP_PRESENT
using System;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting.APIUpdating;
using FloatParameter = UnityEngine.Perception.Randomization.Parameters.FloatParameter;
using Vector2Parameter = UnityEngine.Perception.Randomization.Parameters.Vector2Parameter;

namespace UnityEngine.Perception.Randomization.VolumeEffects
{
    [Serializable]
    [MovedFrom("UnityEngine.Perception.Internal.Effects")]
    public class LensDistortionEffect : VolumeEffect
    {
        public override string displayName => "Lens Distortion";

        public FloatParameter intensity = new FloatParameter { value = new UniformSampler(-1f, 1f) };
        public FloatParameter xMultiplier = new FloatParameter { value = new UniformSampler(0, 1f) };
        public FloatParameter yMultiplier = new FloatParameter { value = new UniformSampler(0, 1f) };
        public Vector2Parameter center = new Vector2Parameter()
        {
            x = new ConstantSampler(0.5f),
            y = new ConstantSampler(0.5f)
        };
        public FloatParameter scale = new FloatParameter()
        {
            value = new UniformSampler(0.01f, 5f)
        };


        LensDistortion m_LensDistortion;
        public LensDistortion lensDistortion => m_LensDistortion = m_LensDistortion ? m_LensDistortion : GetVolumeComponent(ref m_LensDistortion);

        public override void SetActive(bool active)
        {
            lensDistortion.active = active;
        }

        public override void RandomizeEffect()
        {
            lensDistortion.intensity.value = intensity.Sample();
            lensDistortion.xMultiplier.value = xMultiplier.Sample();
            lensDistortion.yMultiplier.value = yMultiplier.Sample();
            lensDistortion.center.value = center.Sample();
            lensDistortion.scale.value = scale.Sample();
        }

        public override void Dispose() {}
    }
}
#endif
