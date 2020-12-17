using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Perception.Randomization.Parameters;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Creates a 2D layer of of evenly spaced GameObjects from a given list of prefabs
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Foreground Object Placement Randomizer")]
    public class ForegroundObjectPlacementRandomizer : Randomizer
    {
        /// <summary>
        /// The Z offset component applied to the generated layer of GameObjects
        /// </summary>
        public float depth;

        /// <summary>
        /// The minimum distance between all placed objects
        /// </summary>
        public float separationDistance = 2f;

        /// <summary>
        /// The size of the 2D area designated for object placement
        /// </summary>
        public Vector2 placementArea;

        /// <summary>
        /// The list of prefabs sample and randomly place
        /// </summary>
        public GameObjectParameter prefabs;

        GameObject m_Container;
        GameObjectOneWayCache m_GameObjectOneWayCache;

        protected override void OnCreate()
        {
            m_Container = new GameObject("Foreground Objects");
            var scenarioTransform = scenario.transform;
            m_Container.transform.parent = scenarioTransform;
            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                scenarioTransform, prefabs.categories.Select(element => element.Item1).ToArray());
        }

        /// <summary>
        /// Generates a foreground layer of objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var seed = scenario.NextRandomState();
            var placementSamples = PoissonDiskSampling.GenerateSamples(
                placementArea.x, placementArea.y, separationDistance, seed);
            var offset = new Vector3(placementArea.x, placementArea.y, 0f) * -0.5f;
            foreach (var sample in placementSamples)
            {
                var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefabs.Sample());
                instance.transform.position = new Vector3(sample.x, sample.y, depth) + offset;
            }
            placementSamples.Dispose();
        }

        /// <summary>
        /// Deletes generated foreground objects after each scenario iteration is complete
        /// </summary>
        protected override void OnIterationEnd()
        {
            m_GameObjectOneWayCache.ResetAllObjects();
        }
    }
}
