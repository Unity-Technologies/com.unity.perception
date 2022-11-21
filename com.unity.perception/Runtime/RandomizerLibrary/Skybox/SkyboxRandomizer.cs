#if HDRP_PRESENT
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Randomizes the HDRI Sky asset of the Volume component attached to target objects.
    /// Used in conjunction with the <see cref="SkyboxRandomizerTag"/>.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.Internal")]
    [AddRandomizerMenu("Perception/Skybox Randomizer")]
    public class SkyboxRandomizer : Randomizer
    {
        /// <summary>
        /// Randomly chooses a skybox from the options provided and assigns it to the HDRI Sky property in the Volume
        /// component. The probability of each skybox being selected can be modified by disabling the
        /// Uniform flag and providing probability values manually.
        /// </summary>
        [Tooltip("Randomly chooses a skybox from the options provided and assigns it to the HDRI Sky property in the Volume component. The probability of each skybox being selected can be modified by disabling the Uniform flag and providing probability values manually.")]
        public CategoricalParameter<Cubemap> skyboxes = new CategoricalParameter<Cubemap>();

        /// <summary>
        /// The number of degrees by which the sampled skybox will be rotated around the Y-axis.
        /// </summary>
        [Tooltip("The number of degrees by which the sampled skybox will be rotated around the Y-axis.")]
        public FloatParameter rotation = new FloatParameter { value = new UniformSampler(0, 360) };

        /// <summary>
        /// At the start of every iteration, randomize the HDRI Skybox and its rotation for each Volume associated with
        /// a <see cref="SkyboxRandomizerTag"/> found in the scene.
        /// </summary>
        protected override void OnIterationStart()
        {
            var tags = tagManager.Query<SkyboxRandomizerTag>();
            var skyboxSampleCount = skyboxes.categories.Count;
            foreach (var tag in tags)
            {
                if (tag.sky != null)
                {
                    if (skyboxSampleCount > 0)
                        tag.sky.hdriSky.Override(skyboxes.Sample());

                    tag.sky.rotation.Override(rotation.Sample());
                }
            }
        }
    }
}
#endif
