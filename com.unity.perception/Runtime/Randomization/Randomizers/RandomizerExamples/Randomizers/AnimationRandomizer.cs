using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers.Tags;
using UnityEngine.Experimental.Perception.Randomization.Samplers;
using UnityEngine.Playables;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Chooses a random of frame of a random clip for a gameobject
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Animation Randomizer")]
    public class AnimationRandomizer : Randomizer
    {
        FloatParameter m_FloatParameter = new FloatParameter{ value = new UniformSampler(0, 1) };
        Dictionary<GameObject, (PlayableGraph, Animator)> m_GraphMap;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_GraphMap = new Dictionary<GameObject, (PlayableGraph, Animator)>();
        }

        (PlayableGraph, Animator) GetGraph(GameObject gameObject)
        {
            if (!m_GraphMap.ContainsKey(gameObject))
            {
                var graph = PlayableGraph.Create();
                graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                var animator = gameObject.GetComponent<Animator>();
                m_GraphMap[gameObject] = (graph, animator);
            }

            return m_GraphMap[gameObject];
        }

        /// <inheritdoc/>
        protected override void OnIterationStart()
        {
            var taggedObjects = tagManager.Query<AnimationRandomizerTag>();
            foreach (var taggedObject in taggedObjects)
            {
                var (graph, animator) = GetGraph(taggedObject.gameObject);

                var tag = taggedObject.GetComponent<AnimationRandomizerTag>();

                var clips = tag.animationClips;
                CategoricalParameter<AnimationClip> param = new AnimationClipParameter();
                param.SetOptions(clips);
                var clip = param.Sample();

                var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
                var playable = AnimationClipPlayable.Create(graph, clip);
                output.SetSourcePlayable(playable);
                var l = clip.length;
                var t = m_FloatParameter.Sample();
                playable.SetTime(t * l);
                playable.SetSpeed(0);
                graph.Play();
            }
        }
    }
}
