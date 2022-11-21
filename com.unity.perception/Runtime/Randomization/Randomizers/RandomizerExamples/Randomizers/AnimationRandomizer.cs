using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers.Tags;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Chooses a random of frame of a random clip for a game object
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Animation Randomizer")]
    [MovedFrom("UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers")]
    public class AnimationRandomizer : Randomizer
    {
        const string k_ClipName = "PlayerIdle";
        const string k_StateName = "Base Layer.RandomState";

        UniformSampler m_Sampler = new UniformSampler();

        void RandomizeAnimation(AnimationRandomizerTag tag)
        {
            if (!tag.gameObject.activeInHierarchy)
                return;

            var animator = tag.gameObject.GetComponent<Animator>();
            animator.applyRootMotion = tag.applyRootMotion;

            var overrider = tag.animatorOverrideController;
            if (overrider != null && tag.animationClips.Count > 0)
            {
                overrider[k_ClipName] = tag.animationClips.Sample();
                animator.Play(k_StateName, 0, m_Sampler.Sample());
            }
        }

        /// <inheritdoc/>
        protected override void OnIterationStart()
        {
            var taggedObjects = tagManager.Query<AnimationRandomizerTag>();
            foreach (var taggedObject in taggedObjects)
            {
                RandomizeAnimation(taggedObject);
            }
        }
    }
}
