using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Perception.GroundTruth;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers.Tags
{
    /// <summary>
    /// Used in conjunction with a <see cref="AnimationRandomizer"/> to select a random animation frame for
    /// the tagged game object
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Perception/RandomizerTags/Animation Randomizer Tag")]
    public class AnimationRandomizerTag : RandomizerTag
    {
        /// <summary>
        /// A list of animation clips from which to choose
        /// </summary>
        public AnimationClipParameter animationClips;

        /// <summary>
        /// Apply the root motion to the animator. If true, if an animation has a rotation translation and/or rotation
        /// that will be applied to the labeled model, which means that the model maybe move to a new position.
        /// If false, then the model will stay at its current position/rotation.
        /// </summary>
        public bool applyRootMotion = false;

    }
}
