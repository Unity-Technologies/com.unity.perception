#if SUBSTANCE_PLUGIN_ENABLED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Substance.Game;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Randomizes the individual or shared substances of all objects in the scene with
    /// <see cref="SubstanceRandomizerTag" /> according to the configuration of the tag. It does so by varying selected
    /// parameters within the constraints defined in the substance itself.
    /// <remarks>
    /// Due to the length of time it takes to render a substance, substances can be randomized less frequently
    /// then other components in a scenario based on the <see cref="updateFrequency"/> variable. Currently,
    /// all types of parameters, except textures and strings, can be randomized.
    /// </remarks>
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Substance Randomizer")]
    [MovedFrom("UnityEngine.Perception.Internal")]
    public class SubstanceRandomizer : Randomizer
    {
        #region Properties
        /// <summary>
        /// Regenerate substance textures every N iterations
        /// </summary>
        [Tooltip("The interval of iterations after which substances are randomized again.")]
        public int updateFrequency = 10;
        /// <summary>
        /// The maximum number of substance graphs randomized per iteration.
        /// </summary>
        [Tooltip("The maximum number of substance graphs randomized per iteration.")]
        public int maxRandomizationsPerIteration = 10;

        bool m_DelayedLastIteration = false;
        // Single parameters used to generate samples for each parameter of corresponding type
        FloatParameter m_FloatParameter = new FloatParameter();
        BooleanParameter m_BoolParameter = new BooleanParameter();
        #endregion

        #region Helper Functions
        /// <summary>
        /// Generate an int sample between <see cref="min" /> and <see cref="max" />.
        /// </summary>
        int SampleInt(int min, int max)
        {
            return min + Mathf.FloorToInt(m_FloatParameter.Sample() * (max - min));
        }

        /// <summary>
        /// Generate a float sample between <see cref="min" /> and <see cref="max" />.
        /// </summary>
        float SampleFloat(float min, float max)
        {
            return min + (max - min) * m_FloatParameter.Sample();
        }

        /// <summary>
        /// Randomly shuffles each array element to a new position.
        /// </summary>
        void ShuffleArray<T>(T[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                var newIndex = SampleInt(0, array.Length - 1);
                var temp = array[newIndex];
                array[newIndex] = array[i];
                array[i] = temp;
            }
        }

        #endregion

        #region Randomizer Lifecycle
        protected override void OnIterationStart()
        {
            // If we are still processing substances, delay iteration.
            if (Substance.Game.Substance.IsProcessing())
            {
                scenario.DelayIteration();
                return;
            }

            // If we delayed this iteration, skip doing anything.
            if (m_DelayedLastIteration)
            {
                m_DelayedLastIteration = false;
                return;
            }

            // If it is not an iteration we want to randomize, skip rendering.
            if (scenario.currentIteration % updateFrequency != 0)
                return;

            var tags = tagManager.Query<SubstanceRandomizerTag>(true);
            var graphs = new Dictionary<SubstanceGraph, SubstanceRandomizerTag>();
            foreach (var tag in tags)
            {
                var graph = tag.runtimeGraph;

                if (graph == null)
                {
                    var targetGameObject = tag.gameObject;
                    Debug.LogError($"Missing graph on SubstanceRandomizerTag on object {targetGameObject} ", targetGameObject);
                    continue;
                }

                graphs[graph] = tag;
            }

            // Pick which substance graphs to randomize (within the maxRandomizationsPerIteration constraint)
            var graphTagPairs = graphs.ToArray();
            ShuffleArray(graphTagPairs);
            var numberOfGraphsToRandomize = Math.Min(graphTagPairs.Length, this.maxRandomizationsPerIteration);
            var selectedGraphTagPairs = graphTagPairs.Take(numberOfGraphsToRandomize).ToArray();

            var sb = new StringBuilder($"Randomization Summary for Iteration #{scenario.currentIteration}");
            sb.Append($"\n  Randomizing {numberOfGraphsToRandomize} out of {graphTagPairs.Length} substance graphs.");

            foreach (var kvp in selectedGraphTagPairs)
            {
                sb.Append($"\n\n  ○ Substance \"{kvp.Key.name}\" from GameObject \"{kvp.Value.name}\"");

                var desiredRandomizationParameters = kvp.Value.parametersToRandomize;
                var substanceGraph = kvp.Key;
                var substanceProperties = substanceGraph.GetInputProperties()
                    .Where(x => desiredRandomizationParameters.Contains(x.name)).ToList();

                foreach (var property in substanceProperties)
                {
                    sb.Append($"\n    • {property.@group} > {property.label}");

                    if (property.name == "$randomseed")
                        substanceGraph.SetInputInt(property.name, SampleInt(0, int.MaxValue));
                    else if (property.name == "$outputsize")
                        substanceGraph.SetInputInt(property.name, SampleInt(128, 1024));

                    switch (property.type)
                    {
                        case SubstanceGraph.InputPropertiesType.Boolean:
                            substanceGraph.SetInputBool(property.name, m_BoolParameter.Sample());
                            break;
                        case SubstanceGraph.InputPropertiesType.Float:
                            substanceGraph.SetInputFloat(property.name, SampleFloat(property.minimum.x, property.maximum.x));
                            break;
                        case SubstanceGraph.InputPropertiesType.Vector2:
                            substanceGraph.SetInputFloat(property.name, SampleFloat(property.minimum.x, property.maximum.x));
                            substanceGraph.SetInputFloat(property.name, SampleFloat(property.minimum.y, property.maximum.y));
                            break;
                        case SubstanceGraph.InputPropertiesType.Vector3:
                            substanceGraph.SetInputFloat(property.name, SampleFloat(property.minimum.x, property.maximum.x));
                            substanceGraph.SetInputFloat(property.name, SampleFloat(property.minimum.y, property.maximum.y));
                            substanceGraph.SetInputFloat(property.name, SampleFloat(property.minimum.z, property.maximum.z));
                            break;
                        case SubstanceGraph.InputPropertiesType.Vector4:
                            substanceGraph.SetInputFloat(property.name, SampleFloat(property.minimum.x, property.maximum.x));
                            substanceGraph.SetInputFloat(property.name, SampleFloat(property.minimum.y, property.maximum.y));
                            substanceGraph.SetInputFloat(property.name, SampleFloat(property.minimum.z, property.maximum.z));
                            substanceGraph.SetInputFloat(property.name, SampleFloat(property.minimum.w, property.maximum.w));
                            break;
                        case SubstanceGraph.InputPropertiesType.Color:
                            var r = m_FloatParameter.Sample();
                            var g = m_FloatParameter.Sample();
                            var b = m_FloatParameter.Sample();
                            substanceGraph.SetInputColor(property.name, new Color(r, g, b, 1.0f));
                            break;
                        case SubstanceGraph.InputPropertiesType.Enum:
                            substanceGraph.SetInputInt(property.name, SampleInt(0, property.enumOptions.Length));
                            break;
                    }
                }

                substanceGraph.RenderSync();
                substanceGraph.QueueForRender();
            }

            Debug.LogFormat(sb.ToString());
            Substance.Game.Substance.RenderSubstancesAsync();
            scenario.DelayIteration();
            m_DelayedLastIteration = true;
        }

        protected override void OnScenarioComplete()
        {
            var tags = tagManager.Query<SubstanceRandomizerTag>(true);
            foreach (var tag in tags)
            {
                var graph = tag.runtimeGraph;
                if (graph != null)
                    tag.runtimeGraph.ResetGeneratedTextures();
            }
        }

        #endregion
    }
}
#endif
