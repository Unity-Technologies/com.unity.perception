using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Creates multiple layers of evenly distributed but randomly placed objects
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Background Object Placement Randomizer")]
    public class BackgroundObjectPlacementRandomizer : Randomizer
    {
        List<GameObject> m_SpawnedObjects = new List<GameObject>();

        /// <summary>
        /// The Z offset component applied to all generated background layers
        /// </summary>
        public float depth;

        /// <summary>
        /// The number of background layers to generate
        /// </summary>
        public int layerCount = 2;

        /// <summary>
        /// The minimum distance between placed background objects
        /// </summary>
        public float separationDistance = 2f;

        /// <summary>
        /// The 2D size of the generated background layers
        /// </summary>
        public Vector2 placementArea;

        /// <summary>
        /// A categorical parameter for sampling random prefabs to place
        /// </summary>
        public GameObjectParameter prefabs;

        /// <summary>
        /// Generates background layers of objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            if (m_SpawnedObjects == null)
                m_SpawnedObjects = new List<GameObject>();

            for (var i = 0; i < layerCount; i++)
            {
                var seed = scenario.GenerateRandomSeedFromIndex(i);
                var placementSamples = PoissonDiskSampling.GenerateSamples(
                    placementArea.x, placementArea.y, separationDistance, seed);
                var offset = new Vector3(placementArea.x, placementArea.y, 0f) * -0.5f;
                var parent = scenario.transform;
                foreach (var sample in placementSamples)
                {
                    var instance = Object.Instantiate(prefabs.Sample(), parent);
                    instance.transform.position = new Vector3(sample.x, sample.y, separationDistance * i + depth) + offset;
                    m_SpawnedObjects.Add(instance);
                }
                placementSamples.Dispose();
            }
        }

        /// <summary>
        /// Deletes generated background objects after each scenario iteration is complete
        /// </summary>
        protected override void OnIterationEnd()
        {
            foreach (var spawnedObject in m_SpawnedObjects)
                Object.Destroy(spawnedObject);
            m_SpawnedObjects.Clear();
        }
    }
}
