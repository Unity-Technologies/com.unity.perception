using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    [Serializable]
    public class ForegroundObjectPlacementRandomizer : Randomizer
    {
        public float depth;
        public float separationDistance = 2f;
        public Vector2 placementArea;
        public GameObjectParameter prefabs;
        List<GameObject> m_SpawnedObjects = new List<GameObject>();

        protected override void OnIterationStart()
        {
            var seed = SamplerUtility.IterateSeed((uint)scenario.currentIteration, SamplerUtility.largePrime);
            var placementSamples = PoissonDiskSampling.GenerateSamples(
                placementArea.x, placementArea.y, separationDistance, seed);
            var offset = new Vector3(placementArea.x, placementArea.y, 0f) * -0.5f;
            var parent = scenario.transform;
            foreach (var sample in placementSamples)
            {
                var instance = Object.Instantiate(prefabs.Sample(), parent);
                instance.transform.position = new Vector3(sample.x, sample.y, depth) + offset;
                m_SpawnedObjects.Add(instance);
            }
            placementSamples.Dispose();
        }

        protected override void OnIterationEnd()
        {
            foreach (var spawnedObject in m_SpawnedObjects)
                Object.Destroy(spawnedObject);
            m_SpawnedObjects.Clear();
        }
    }
}
