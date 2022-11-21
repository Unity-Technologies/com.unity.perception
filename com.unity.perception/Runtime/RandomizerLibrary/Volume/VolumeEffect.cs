using System;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization
{
    /// <summary>
    /// A helper class used to generate post-processing effects for the <see cref="VolumeRandomizer" />.
    /// </summary>
    /// <example>
    /// <see cref="UnityEngine.Perception.Randomization.VolumeEffects.BloomEffect" />,
    /// <see cref="UnityEngine.Perception.Randomization.VolumeEffects.DepthOfFieldEffect" />,
    /// <see cref="UnityEngine.Perception.Randomization.VolumeEffects.LensDistortionEffect" />
    /// </example>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.Internal")]
    public abstract class VolumeEffect : IDisposable
    {
        /// <summary>
        /// The effect name displayed in the Inspector UI.
        /// </summary>
        public abstract string displayName { get; }

        /// <summary>
        /// The volume whose post-processing effect this randomizer is targeting.
        /// </summary>
        [HideInInspector]
        public Volume targetVolume;
        VolumeProfile m_TargetVolumeProfile;
        VolumeProfile targetVolumeProfile
        {
            get
            {
                if (targetVolume == null)
                    throw new Exception($"{nameof(targetVolume)} property unassigned.");

                if (m_TargetVolumeProfile == null)
                    m_TargetVolumeProfile = targetVolume.profile;

                return m_TargetVolumeProfile;
            }
        }

        /// <summary>
        /// Gets the Volume Component in the <see cref="targetVolumeProfile" />. If the Volume Component does not exist,
        /// it will create one and return a reference to the newly created component.
        /// </summary>
        /// <param name="volumeComponent">A volume component such as Bloom, Depth of Field, etc.</param>
        /// <typeparam name="T">A class that inherits from VolumeComponent such as Bloom, DepthOfField</typeparam>
        /// <returns>A volume component from the <see cref="targetVolumeProfile"/></returns>
        protected T GetVolumeComponent<T>(ref T volumeComponent) where T : VolumeComponent
        {
            if (volumeComponent == null)
            {
                if (targetVolumeProfile.TryGet<T>(out volumeComponent) == false)
                {
                    volumeComponent = targetVolumeProfile.Add<T>(true);
                }
            }

            return volumeComponent;
        }

        /// <summary>
        /// Removes the existing Volume Profile of the <see cref="targetVolume" />.
        /// </summary>
        protected void DestroyProfile()
        {
            if (targetVolume.HasInstantiatedProfile())
            {
                Object.Destroy(targetVolumeProfile);
                targetVolume.profile = null;
            }
        }

        /// <summary>
        /// A placeholder function to enable or disable the Volume Effect.
        /// </summary>
        /// <param name="active">Whether the volume randomization is enabled or disabled</param>
        /// <remarks>
        /// It is up to the child class to define what active or inactive means for the specific Volume Effect.
        /// </remarks>
        public abstract void SetActive(bool active);
        /// <summary>
        /// At the beginning of the Scenario, set up required information for the Volume Effect to function (such as
        /// adding the appropriate <see cref="VolumeComponent"/> to the <see cref="targetVolumeProfile"/>)
        /// </summary>
        public virtual void SetupEffect() {}
        /// <summary>
        /// At each iteration, randomize the parameters for the post-processing effect controlled by this
        /// <see cref="VolumeEffect" />.
        /// </summary>
        public abstract void RandomizeEffect();
        /// <summary>
        /// At the end of the scenario, rollback any permanent changes to the scene.
        /// </summary>
        public abstract void Dispose();
    }
}
