using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers.Tags
{
    /// <summary>
    /// Used in conjunction with a <see cref="AnimationRandomizer"/> to select a random animation frame for
    /// the tagged game object
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Perception/RandomizerTags/Animation Randomizer Tag")]
    [MovedFrom("UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers.Tags")]
    public class AnimationRandomizerTag : RandomizerTag
    {
        /// <summary>
        /// A list of animation clips from which to choose
        /// </summary>
        public CategoricalParameter<AnimationClip> animationClips;

        /// <summary>
        /// Apply the root motion to the animator. If true, if an animation has a rotation translation and/or rotation
        /// that will be applied to the labeled model, which means that the model maybe move to a new position.
        /// If false, then the model will stay at its current position/rotation.
        /// </summary>
        public bool applyRootMotion = false;

        /// <summary>
        /// Gets the animation override controller for an animation randomization. The controller is loaded from
        /// resources.
        /// </summary>
        public AnimatorOverrideController animatorOverrideController
        {
            get
            {
                if (m_Controller == null)
                {
                    var animator = gameObject.GetComponent<Animator>();
                    var runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("AnimationRandomizerController");
                    m_Controller = new AnimatorOverrideController(runtimeAnimatorController);
                    animator.runtimeAnimatorController = m_Controller;
                }

                return m_Controller;
            }
        }

        AnimatorOverrideController m_Controller;
    }
}
