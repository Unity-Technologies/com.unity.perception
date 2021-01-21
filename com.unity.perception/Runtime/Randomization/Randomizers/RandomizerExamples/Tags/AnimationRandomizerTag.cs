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
        public AnimationClip[] animationClips;
    }
}
