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
    public class BloomEffect : VolumeEffect
    {
        public override string displayName => "Bloom";

        public FloatParameter threshold = new FloatParameter { value = new UniformSampler(0f, 0.75f) };
        public FloatParameter intensity = new FloatParameter { value = new UniformSampler(0f, 1f) };
        public FloatParameter scatter = new FloatParameter { value = new UniformSampler(0f, 1f) };

        Bloom m_Bloom;
        public Bloom bloom => m_Bloom = m_Bloom ? m_Bloom : GetVolumeComponent(ref m_Bloom);

        public override void SetActive(bool active)
        {
            bloom.active = active;
        }

        public override void RandomizeEffect()
        {
            bloom.threshold.value = threshold.Sample();
            bloom.intensity.value = intensity.Sample();
            bloom.scatter.value = scatter.Sample();
        }

        public override void Dispose() {}
    }
}
#endif
