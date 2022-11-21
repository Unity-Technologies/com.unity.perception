#if HDRP_PRESENT
using System;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.VolumeEffects;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// The <see cref="VolumeRandomizer" /> randomizes the Volume attached to the GameObject which has this
    /// <see cref="VolumeRandomizerTag" /> component attached to it.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.Internal")]
    [AddRandomizerMenu("Perception/RandomizerTags/Volume Randomizer Tag")]
    [RequireComponent(typeof(Volume))]
    public class VolumeRandomizerTag : RandomizerTag
    {
        /// <summary>
        /// The list of volume randomizations supported by the VolumeRandomizer
        /// </summary>
        public static readonly Type[] SupportedEffects =
        {
            typeof(BloomEffect), typeof(ExposureEffect), typeof(DepthOfFieldEffect), typeof(CameraTypeEffect),
            typeof(MotionBlurEffect), typeof(LensDistortionEffect)
        };

        /// <summary>
        /// List of volume randomizations currently being used by the <see cref="VolumeRandomizer" />
        /// </summary>
        [SerializeReference]
        public List<VolumeEffect> usedEffects = new List<VolumeEffect>() {};

        /// <summary>
        /// The independent chance for each volume randomization/effect to be enabled.
        /// </summary>
        [SerializeField]
        public BooleanParameter enableEffect = new BooleanParameter() {
            threshold = 0.5f
        };

        Volume m_Volume;

        /// <summary>
        /// Calls the SetupEffect function for each volume randomization/effect.
        /// </summary>
        public void Setup()
        {
            m_Volume = GetComponent<Volume>();
            foreach (var volFx in usedEffects)
            {
                volFx.targetVolume = m_Volume;
                volFx.SetupEffect();
            }
        }

        /// <summary>
        /// Calls the RandomizeEffect function for each volume randomization/effect.
        /// </summary>
        public void Randomize()
        {
            foreach (var volFx in usedEffects)
            {
                var effectIsActive = enableEffect.Sample();
                volFx.SetActive(true);

                if (effectIsActive)
                    volFx.RandomizeEffect();
            }
        }

        /// <summary>
        /// Calls the Dispose function for each volume randomization/effect.
        /// </summary>
        public void Dispose()
        {
            foreach (var volFx in usedEffects)
            {
                volFx.Dispose();
            }
        }
    }
}
#endif
