using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// This randomizer is used to facilitate the randomization of the position, rotation, and scale of GameObjects
    /// tagged with the <see cref="TransformRandomizerTag"/>.
    /// </summary>
    [AddRandomizerMenu("Perception/RandomizerTags/Transform Randomizer")]
    [MovedFrom("UnityEngine.Perception.Internal")]
    public class TransformRandomizer : Randomizer
    {
        /// <summary>
        /// At each iteration, randomize objects in the scene as per the specific configurations
        /// in their attached <see cref="TransformRandomizerTag" />.
        /// </summary>
        protected override void OnIterationStart()
        {
            var tags = tagManager.Query<TransformRandomizerTag>();
            foreach (var tag in tags)
            {
                tag.Randomize();
            }
        }
    }
}
