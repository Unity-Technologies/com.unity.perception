using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// This Randomizer is used in conjunction with the <see cref="LightRandomizerTag"/>, which facilitates the
    /// randomization of various light aspects, including temperature, color, and intensity.
    /// </summary>
    [AddRandomizerMenu("Perception/Light Randomizer")]
    [MovedFrom("UnityEngine.Perception.Internal")]
    public class LightRandomizer : Randomizer
    {
        /// <summary>
        /// At the start of each iteration, call the Randomize function on all found <see cref="LightRandomizerTag"/>'s.
        /// </summary>
        protected override void OnIterationStart()
        {
            foreach (var tag in tagManager.Query<LightRandomizerTag>())
            {
                tag.Randomize();
            }
        }
    }
}
