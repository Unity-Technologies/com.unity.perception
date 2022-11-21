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
    public class DepthOfFieldEffect : VolumeEffect
    {
        public override string displayName => "Depth of Field";

        public FloatParameter nearFocusStart = new FloatParameter { value = new UniformSampler(1, 500) };
        public FloatParameter nearFocusEnd = new FloatParameter { value = new UniformSampler(1, 500) };
        public FloatParameter farFocusStart = new FloatParameter { value = new UniformSampler(1, 500) };
        public FloatParameter farFocusEnd = new FloatParameter { value = new UniformSampler(1, 500) };

        DepthOfField m_Effect;
        public DepthOfField depthOfField => m_Effect = m_Effect ? m_Effect : GetVolumeComponent(ref m_Effect);

        public override void SetupEffect()
        {
            depthOfField.focusMode.value = DepthOfFieldMode.Manual;
        }

        public override void SetActive(bool active)
        {
            depthOfField.active = active;
        }

        public override void RandomizeEffect()
        {
            depthOfField.nearFocusStart.value = nearFocusStart.Sample();
            depthOfField.nearFocusEnd.value = nearFocusEnd.Sample();
            depthOfField.farFocusStart.value = farFocusStart.Sample();
            depthOfField.farFocusEnd.value = farFocusEnd.Sample();
        }

        public override void Dispose() {}
    }
}
#endif
