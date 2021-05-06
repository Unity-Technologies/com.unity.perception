using System;
using System.Linq;
using SynthDet.Randomizers;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.Utilities;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
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
        [Tooltip("The Z offset applied to positions of all placed objects.")]
        public float depth;

        /// <summary>
        /// The minimum distance between all placed objects
        /// </summary>
        [Tooltip("The minimum distance between the centers of the placed objects.")]
        public float separationDistance = 2f;

        /// <summary>
        /// The size of the 2D area designated for object placement
        /// </summary>
        [Tooltip("The width and height of the area in which objects will be placed. These should be positive numbers and sufficiently large in relation with the Separation Distance specified.")]
        public Vector2 placementArea;

        /// <summary>
        /// The list of prefabs sample and randomly place
        /// </summary>
        [Tooltip("The list of Prefabs to be placed by this Randomizer.")]
        public GameObjectParameter prefabs;

        GameObject m_Container;
        GameObjectOneWayCache m_GameObjectOneWayCache;

        /// <summary>
        /// Enables object bounds normalization across all included objects, so that they all have a similar size before any further scale adjustments.
        /// </summary>
        [Tooltip("Enable this to normalize mesh sizes across all included objects, so that they all have a similar size before any further scale adjustments are applied during randomization. Note that this flag greatly influences the size of the objects on the screen, so any scale randomization will need to be adjusted based on the state of this flag.")]
        public bool normalizeObjectBounds;
        /// <inheritdoc/>
        protected override void OnAwake()
        {
            m_Container = new GameObject("Foreground Objects");
            m_Container.transform.parent = scenario.transform;
            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                m_Container.transform, prefabs.categories.Select(element => element.Item1).ToArray());
        }

        /// <summary>
        /// Generates a foreground layer of objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var seed = SamplerState.NextRandomState();
            var placementSamples = PoissonDiskSampling.GenerateSamples(
                placementArea.x, placementArea.y, separationDistance, seed);
            var offset = new Vector3(placementArea.x, placementArea.y, 0f) * -0.5f;
            foreach (var sample in placementSamples)
            {
                var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefabs.Sample());

                if (normalizeObjectBounds)
                {
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localScale = Vector3.one;
                    instance.transform.localRotation = Quaternion.identity;
                    var bounds = ComputeBounds(instance);

                    instance.transform.localPosition = new Vector3(sample.x, sample.y, depth) + offset - bounds.center;
                    var scale = instance.transform.localScale;
                    var magnitude = bounds.extents.magnitude;
                    scale.Scale(new Vector3(1/magnitude, 1/magnitude, 1/magnitude));
                    instance.transform.localScale = scale;
                }
                else
                {
                    instance.transform.position = new Vector3(sample.x, sample.y, depth) + offset;
                }

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

        static NativeArray<Bounds> ComputeObjectBounds(GameObject[] prefabs)
        {
            var objectBounds = new NativeArray<Bounds>(prefabs.Length, Allocator.TempJob);
            for (int i = 0; i < prefabs.Length; i++)
            {
                var bounds = ComputeBounds(prefabs[i]);
                //assume objects will be aligned at origin
                bounds.center = Vector3.zero;
                objectBounds[i] = bounds;
            }

            return objectBounds;
        }

        static Bounds ComputeBounds(GameObject gameObject)
        {
            var bounds = ComputeBoundsUnchecked(gameObject);
            if (!bounds.IsValid)
                throw new ArgumentException($"GameObject {gameObject.name} must have a MeshFilter in its hierarchy.");

            var result = new Bounds();
            result.SetMinMax(bounds.Min, bounds.Max);
            return result;
        }

        static SynthDetMinMaxAABB ComputeBoundsUnchecked(GameObject gameObject)
        {
            SynthDetMinMaxAABB aabb = new SynthDetMinMaxAABB(
                new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity));
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                var bounds = meshFilter.sharedMesh.bounds;
                aabb = SynthDetMinMaxAABB.CreateFromCenterAndExtents(bounds.center, bounds.extents);
            }

            var transform = gameObject.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                var childAabb = ComputeBoundsUnchecked(transform.GetChild(i).gameObject);
                aabb.Encapsulate(childAabb);
            }

            aabb = SynthDetMinMaxAABB.Transform(float4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale), aabb);
            return aabb;
        }
    }
}
