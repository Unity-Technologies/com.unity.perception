#if HDRP_PRESENT
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Randomizes the Volumes attached to each <see cref="VolumeRandomizerTag" /> in the Scene based on the
    /// configuration specified in the <see cref="VolumeRandomizerTag" />.
    /// </summary>
    [AddRandomizerMenu("Perception/Volume Randomizer")]
    [MovedFrom("UnityEngine.Perception.Internal")]
    public class VolumeRandomizer : Randomizer
    {
        /// <inheritdoc />
        protected override void OnScenarioStart()
        {
            foreach (var tag in tagManager.Query<VolumeRandomizerTag>())
            {
                tag.Setup();
            }
        }

        /// <inheritdoc />
        protected override void OnIterationStart()
        {
            foreach (var tag in tagManager.Query<VolumeRandomizerTag>())
            {
                tag.Randomize();
            }
        }

        /// <inheritdoc />
        protected override void OnScenarioComplete()
        {
            foreach (var tag in tagManager.Query<VolumeRandomizerTag>())
            {
                tag.Dispose();
            }
        }
    }
}
#endif
