using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Perception.Randomization.Utilities;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.LabelManagement
{
    /// <summary>
    /// Manages the registration of <see cref="Labeling"/> components
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class LabelManager
    {
        static readonly int k_InstanceIdIndex = Shader.PropertyToID("_InstanceIdIndex");

        int m_PreviousFrameIndex = -1;
        uint m_NextInstanceId = 1;
        List<IGroundTruthGenerator> m_ActiveGenerators = new List<IGroundTruthGenerator>();
        LinkedHashSet<Labeling> m_LabelsPendingRegistration = new LinkedHashSet<Labeling>();
        LinkedHashSet<Labeling> m_RegisteredLabels = new LinkedHashSet<Labeling>();

        /// <summary>
        /// Returns the active LabeledObjectsManager instance
        /// </summary>
        public static LabelManager singleton { get; } = new LabelManager();

        /// <summary>
        /// The unique ids assigned to each registered labeled object.
        /// This list is updated each frame right before rendering starts.
        /// </summary>
        public NativeList<uint> instanceIds;

        /// <summary>
        /// The unique segmentation colors assigned to each registered labeled object.
        /// This list is updated each frame right before rendering starts.
        /// </summary>
        /// <note>
        /// This list is append only and will not shrink to reflect the
        /// current number of registered labeled objects in the scene.
        /// </note>
        public NativeList<Color32> instanceSegmentationColors;

        /// <summary>
        /// Used to load/cache materials when using non-allocating calls for GetMaterials and GetSharedMaterials
        /// </summary>
        static List<Material> s_ObjectMaterials = new List<Material>();

        /// <summary>
        /// Returns the set of registered Labeling components
        /// </summary>
        public IEnumerable<Labeling> registeredLabels
        {
            get
            {
                RegisterPendingLabels();
                return m_RegisteredLabels;
            }
        }

        LabelManager()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif
            Initialize();
            Application.quitting += Cleanup;
        }

        /// <summary>
        /// Initialize the LabelManager.
        /// </summary>
        void Initialize()
        {
            instanceIds = new NativeList<uint>(16, Allocator.Persistent);
            instanceIds.Add(0);

            instanceSegmentationColors = new NativeList<Color32>(16, Allocator.Persistent);
            instanceSegmentationColors.Add(new Color32(0, 0, 0, 255));

            RenderPipelineManager.beginFrameRendering += BeginFrameRendering;
        }

        /// <summary>
        /// Clean up the LabelManager's allocated resources.
        /// </summary>
        void Cleanup()
        {
            RenderPipelineManager.beginFrameRendering -= BeginFrameRendering;
            if (instanceIds.IsCreated)
                instanceIds.Dispose();
            if (instanceSegmentationColors.IsCreated)
                instanceSegmentationColors.Dispose();
        }

        /// <summary>
        /// Registers all pending labels.
        /// Called once per frame during LateUpdate by the <see cref="PerceptionUpdater"/>.
        /// </summary>
        public void RegisterPendingLabels()
        {
            foreach (var unregisteredLabel in m_LabelsPendingRegistration)
            {
                if (m_RegisteredLabels.Contains(unregisteredLabel))
                    continue;

                var instanceId = unregisteredLabel.instanceId;

                RecursivelyInitializeGameObjects(
                    unregisteredLabel.gameObject,
                    new MaterialPropertyBlock(),
                    unregisteredLabel,
                    instanceId);

                m_RegisteredLabels.Add(unregisteredLabel);
            }

            m_LabelsPendingRegistration.Clear();
        }

        /// <summary>
        /// Activates the given <see cref="IGroundTruthGenerator"/>.
        /// <see cref="IGroundTruthGenerator.SetupMaterialProperties"/> will be called for all
        /// <see cref="MeshRenderer"/> instances under each object containing a <see cref="Labeling"/> component.
        /// </summary>
        /// <typeparam name="T">The type of generator to add.</typeparam>
        public void Activate<T>() where T : IGroundTruthGenerator, new()
        {
            // Check if an instance of the given generator type has already been activated.
            var type = typeof(T);
            foreach (var activeGenerator in m_ActiveGenerators)
                if (activeGenerator.GetType() == type)
                    return;

            m_ActiveGenerators.Add(new T());
            foreach (var label in m_RegisteredLabels)
                RecursivelyInitializeGameObjects(label.gameObject, new MaterialPropertyBlock(), label, label.instanceId);
        }

        /// <summary>
        /// Activates the given <see cref="IGroundTruthGenerator"/>.
        /// <see cref="IGroundTruthGenerator.SetupMaterialProperties"/> will be called for all
        /// <see cref="MeshRenderer"/> instances under each object containing a <see cref="Labeling"/> component.
        /// </summary>
        /// <param name="generator">The generator to register</param>
        public void Activate(IGroundTruthGenerator generator)
        {
            m_ActiveGenerators.Add(generator);
            foreach (var label in m_RegisteredLabels)
                RecursivelyInitializeGameObjects(label.gameObject, new MaterialPropertyBlock(), label, label.instanceId);
        }

        /// <summary>
        /// Deactivates the given type of <see cref="IGroundTruthGenerator"/>.
        /// It will no longer receive calls when <see cref="Labeling"/> instances are created.
        /// </summary>
        /// <typeparam name="T">The type of generator to remove.</typeparam>
        public void Deactivate<T>()
        {
            foreach (var generator in m_ActiveGenerators)
                if (generator is T)
                    m_ActiveGenerators.Remove(generator);
        }

        /// <summary>
        /// Deactivates the given <see cref="IGroundTruthGenerator"/>.
        /// It will no longer receive calls when <see cref="Labeling"/> instances are created.
        /// </summary>
        /// <param name="generator">The generator to deactivate</param>
        /// <returns>
        /// True if the generator was successfully removed.
        /// False if generator was not active.
        /// </returns>
        public bool Deactivate(IGroundTruthGenerator generator)
        {
            return m_ActiveGenerators.Remove(generator);
        }

        /// <summary>
        /// Unregisters a labeling component
        /// </summary>
        /// <param name="labeling">the component to unregister</param>
        internal void Unregister(Labeling labeling)
        {
            m_LabelsPendingRegistration.Remove(labeling);
            m_RegisteredLabels.Remove(labeling);
            SetInstanceIndexRecursively(labeling.gameObject, labeling, 0, new MaterialPropertyBlock());
        }

        /// <summary>
        /// Refresh ground truth generation for the labeling of a particular GameObject. This is necessary when the
        /// list of labels changes or when renderers or materials change on objects in the hierarchy.
        /// </summary>
        /// <param name="labeling">the component to refresh</param>
        internal void RefreshLabeling(Labeling labeling)
        {
            m_RegisteredLabels.Remove(labeling);
            m_LabelsPendingRegistration.Add(labeling);
        }

        static void GetRendererMaterials(Renderer renderer, ref List<Material> cache)
        {
            renderer.GetSharedMaterials(cache);
            // sometimes sharedMaterials is updated "slower" than just "materials"
            // in that case, the returned sharedMaterials will be of the correct length but pointing to null values
            if (cache.Count > 0 && cache[0] == null)
                renderer.GetMaterials(cache);
        }

        /// <summary>
        /// Returns the next instance Id
        /// </summary>
        /// <returns></returns>
        internal uint GetNextInstanceId()
        {
            return m_NextInstanceId++;
        }

        /// <summary>
        /// Recursively initializes Renderer components on GameObjects with Labeling components
        /// </summary>
        /// <param name="gameObject">The parent GameObject being initialized</param>
        /// <param name="mpb">A reusable material property block</param>
        /// <param name="labeling">The labeling component attached to the current gameObject</param>
        /// <param name="instanceId">The perception specific instanceId assigned to the current gameObject</param>
        void RecursivelyInitializeGameObjects(
            GameObject gameObject, MaterialPropertyBlock mpb, Labeling labeling, uint instanceId)
        {
            s_ObjectMaterials.Clear();

            if (gameObject.TryGetComponent<Terrain>(out var terrain))
            {
                terrain.GetSplatMaterialPropertyBlock(mpb);
                foreach (var pass in m_ActiveGenerators)
                {
                    if (labeling.enabled)
                    {
                        pass.SetupMaterialProperties(mpb, null, labeling, terrain.materialTemplate, instanceId);
                    }
                    else
                    {
                        pass.ClearMaterialProperties(mpb, null, labeling, instanceId);
                    }
                }
                terrain.SetSplatMaterialPropertyBlock(mpb);
            }

            if (gameObject.TryGetComponent<Renderer>(out var renderer))
            {
                GetRendererMaterials(renderer, ref s_ObjectMaterials);
                for (var i = 0; i < s_ObjectMaterials.Count; i++)
                {
                    var material = s_ObjectMaterials[i];
                    renderer.GetPropertyBlock(mpb, i);
                    foreach (var pass in m_ActiveGenerators)
                    {
                        if (labeling.enabled)
                        {
                            pass.SetupMaterialProperties(mpb, renderer, labeling, material, instanceId);
                        }
                        else
                        {
                            pass.ClearMaterialProperties(mpb, renderer, labeling, instanceId);
                        }
                    }
                    renderer.SetPropertyBlock(mpb, i);
                }
            }

            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                if (child.TryGetComponent<Labeling>(out _))
                    continue;

                RecursivelyInitializeGameObjects(child, mpb, labeling, instanceId);
            }
        }

        void BeginFrameRendering(ScriptableRenderContext ctx, Camera[] cameras)
        {
            if (m_PreviousFrameIndex == Time.frameCount)
                return;
            m_PreviousFrameIndex = Time.frameCount;

            var labeledObjects = registeredLabels.ToList();
            instanceIds.Resize(labeledObjects.Count + 1, NativeArrayOptions.ClearMemory);

            for (var instanceIdIndex = 1; instanceIdIndex < instanceIds.Length; instanceIdIndex++)
            {
                var labelingComponent = labeledObjects[instanceIdIndex - 1];
                instanceIds[instanceIdIndex] = labelingComponent.instanceId;
                SetInstanceIndexRecursively(
                    labelingComponent.gameObject, labelingComponent, (uint)instanceIdIndex, new MaterialPropertyBlock());
            }

            if (instanceIds.Length > instanceSegmentationColors.Length)
            {
                var oldLength = instanceSegmentationColors.Length;
                instanceSegmentationColors.Resize(instanceIds.Length, NativeArrayOptions.ClearMemory);
                for (var i = oldLength; i < instanceIds.Length; i++)
                {
                    InstanceIdToColorMapping.TryGetColorFromInstanceId((uint)i, out var segmentationColor);
                    instanceSegmentationColors[i] = segmentationColor;
                }
            }
        }

        static void SetInstanceIndexRecursively(
            GameObject gameObject, Labeling labeling, uint instanceId, MaterialPropertyBlock mpb)
        {
            if (gameObject.TryGetComponent<Terrain>(out var terrain))
            {
                terrain.GetSplatMaterialPropertyBlock(mpb);
                if (labeling.enabled)
                    mpb.SetInt(k_InstanceIdIndex, (int)instanceId);
                else
                    mpb.SetInt(k_InstanceIdIndex, 0);
                terrain.SetSplatMaterialPropertyBlock(mpb);
            }

            if (gameObject.TryGetComponent<Renderer>(out var renderer))
            {
                GetRendererMaterials(renderer, ref s_ObjectMaterials);
                for (var i = 0; i < s_ObjectMaterials.Count; i++)
                {
                    renderer.GetPropertyBlock(mpb, i);
                    if (labeling.enabled)
                        mpb.SetInt(k_InstanceIdIndex, (int)instanceId);
                    else
                        mpb.SetInt(k_InstanceIdIndex, 0);

                    renderer.SetPropertyBlock(mpb, i);
                }
            }

            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                if (child.TryGetComponent<Labeling>(out _) == false)
                {
                    SetInstanceIndexRecursively(child, labeling, instanceId, mpb);
                }
            }
        }
    }
}
