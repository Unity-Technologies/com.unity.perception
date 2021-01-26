using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers.Tags;
using UnityEngine.Experimental.Perception.Randomization.Samplers;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Playables;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Chooses a random of frame of a random clip for a game object
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Animation Randomizer")]
    public class AnimationRandomizer : Randomizer
    {
        FloatParameter m_FloatParameter = new FloatParameter{ value = new UniformSampler(0, 1) };

        class CachedData
        {
            public PlayableGraph graph;
            public AnimationPlayableOutput output;
            public Dictionary<string, AnimationClipPlayable> playables = new Dictionary<string, AnimationClipPlayable>();
            public PoseStateGroundTruthInfo poseState;
        }
        Dictionary<GameObject, CachedData> m_CacheMap;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_CacheMap = new Dictionary<GameObject, CachedData>();
        }

        CachedData InitializeCacheData(AnimationRandomizerTag tag)
        {
            var cachedData = new CachedData { graph = PlayableGraph.Create() };
            cachedData.graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var animator = tag.gameObject.GetComponent<Animator>();
            animator.applyRootMotion = tag.applyRootMotion;

            cachedData.output = AnimationPlayableOutput.Create(cachedData.graph, "Animation", animator);

            for (var i = 0; i < tag.animationClips.GetCategoryCount(); i++)
            {
                var clip = tag.animationClips.GetCategory(i);
                var playable = AnimationClipPlayable.Create(cachedData.graph, clip.animationClip);
                cachedData.playables[clip.animationClip.name] = playable;
            }

            cachedData.poseState = tag.gameObject.GetComponent<PoseStateGroundTruthInfo>();
            if (cachedData.poseState == null)
            {
                cachedData.poseState = tag.gameObject.AddComponent<PoseStateGroundTruthInfo>();
            }

            return cachedData;
        }

        CachedData GetGraph(AnimationRandomizerTag tag)
        {
            if (!m_CacheMap.ContainsKey(tag.gameObject))
            {
                m_CacheMap[tag.gameObject] = InitializeCacheData(tag);

            }
            return m_CacheMap[tag.gameObject];
        }

        /// <inheritdoc/>
        protected override void OnIterationStart()
        {
            var taggedObjects = tagManager.Query<AnimationRandomizerTag>();
            foreach (var taggedObject in taggedObjects)
            {
                var tag = taggedObject.GetComponent<AnimationRandomizerTag>();
                var cached = GetGraph(tag);

                var clips = tag.animationClips;
                var animationPoseLabel = clips.Sample();
                var clip = animationPoseLabel.animationClip;

                var playable = cached.playables[clip.name];
                cached.output.SetSourcePlayable(cached.playables[clip.name]);
                var t = m_FloatParameter.Sample() * clip.length;

                playable.SetTime(t);
                playable.SetSpeed(0);

                cached.graph.Play();

                cached.poseState.poseState = animationPoseLabel.GetPoseAtTime(t);
            }
        }

        protected override void OnScenarioComplete()
        {
            foreach (var cache in m_CacheMap.Values)
            {
                foreach (var p in cache.playables.Values)
                    cache.graph.DestroyPlayable(p);

                cache.graph.DestroyOutput(cache.output);
                cache.graph.Destroy();
            }
        }
    }
}
