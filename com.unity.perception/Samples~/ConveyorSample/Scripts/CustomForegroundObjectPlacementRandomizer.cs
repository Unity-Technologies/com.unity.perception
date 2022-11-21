using System;
using System.Linq;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Creates a 2D layer of of evenly spaced GameObjects from a given list of prefabs
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Custom Foreground Object Placement Randomizer")]
    public class CustomForegroundObjectPlacementRandomizer : Randomizer
    {
        GameObject m_Container;
        GameObjectOneWayCache m_GameObjectOneWayCache;
        Vector3 m_BeltSize;

        /// <summary>
        /// The conveyor belt GameObject.
        /// </summary>
        [Tooltip("The conveyor belt GameObject.")]
        public GameObject conveyorBelt;

        /// <summary>
        /// The Z offset component applied to the generated layer of GameObjects.
        /// </summary>
        [Tooltip("The height from which objects are spawned above the conveyor belt.")]
        [Range(0.2f, 0.75f)]
        public float dropHeight = 0.25f;

        /// <summary>
        /// The minimum distance between all placed objects.
        /// </summary>
        [Tooltip("The minimum distance between the centers of the placed objects.")]
        [Range(0.1f, 1f)]
        public float separationDistance = 2f;

        /// <summary>
        /// The closest distance objects can be placed towards to edges of the conveyor belt.
        /// </summary>
        [Tooltip("The closest distance objects can be placed towards to edges of the conveyor belt.")]
        [Range(0, 1f)]
        public float offsetFromEdges = 0.25f;

        /// <summary>
        /// The list of prefabs sample and randomly place.
        /// </summary>
        [Tooltip("The list of Prefabs to be placed by this Randomizer.")]
        public CategoricalParameter<GameObject> prefabs;

        /// <inheritdoc/>
        protected override void OnScenarioStart()
        {
            var collider = conveyorBelt.GetComponentInChildren<Collider>();
            m_BeltSize = collider.bounds.size;

            m_Container = new GameObject("Foreground Objects");
            m_Container.transform.parent = scenario.transform;
            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                m_Container.transform,
                prefabs.categories.Select(element => element.Item1).ToArray(),
                this);
        }

        /// <summary>
        /// Generates a foreground layer of objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var placementArea = new Vector2(m_BeltSize.x - offsetFromEdges, m_BeltSize.z - offsetFromEdges);

            var seed = SamplerState.NextRandomState();
            var placementSamples = PoissonDiskSampling.GenerateSamples(
                placementArea.x, placementArea.y, separationDistance, seed);
            var offset = new Vector3(placementArea.x, 0f , placementArea.y) * -0.5f;
            foreach (var sample in placementSamples)
            {
                var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefabs.Sample());
                var rb = instance.GetComponent<Rigidbody>();
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                instance.transform.Rotate(Random.Range(10, 350), Random.Range(10, 350), Random.Range(10, 350));
                instance.transform.position = new Vector3(sample.x, dropHeight , sample.y) + offset;
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
