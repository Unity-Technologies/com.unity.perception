using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Randomizers;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Manages the registration of <see cref="Labeling"/> components
    /// </summary>
    [DefaultExecutionOrder(1)]
    public class LabelManager
    {
        /// <summary>
        /// Returns the active LabeledObjectsManager instance
        /// </summary>
        public static LabelManager singleton { get; } = new LabelManager();

        const uint k_StartingIndex = 1;
        uint m_NextObjectIndex = k_StartingIndex;
        List<IGroundTruthGenerator> m_ActiveGenerators = new List<IGroundTruthGenerator>();
        LinkedHashSet<Labeling> m_LabelsPendingRegistration = new LinkedHashSet<Labeling>();
        LinkedHashSet<Labeling> m_RegisteredLabels = new LinkedHashSet<Labeling>();

        /// <summary>
        /// Returns the set of registered Labeling components
        /// </summary>
        public IEnumerable<Labeling> registeredLabels => m_RegisteredLabels;

        /// <summary>
        /// Registers all pending labels.
        /// Called once per frame during LateUpdate by the <see cref="PerceptionUpdater"/>.
        /// </summary>
        public void RegisterPendingLabels()
        {
            if (m_RegisteredLabels.Count == 0)
                m_NextObjectIndex = k_StartingIndex;

            foreach (var unregisteredLabel in m_LabelsPendingRegistration)
            {
                if (m_RegisteredLabels.Contains(unregisteredLabel))
                    continue;

                var instanceId = m_NextObjectIndex++;

                RecursivelyInitializeGameObjects(
                    unregisteredLabel.gameObject,
                    new MaterialPropertyBlock(),
                    unregisteredLabel,
                    instanceId);

                unregisteredLabel.SetInstanceId(instanceId);
                m_RegisteredLabels.Add(unregisteredLabel);
            }

            m_LabelsPendingRegistration.Clear();
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
        /// Deactivates the given <see cref="IGroundTruthGenerator"/>.
        /// It will no longer receive calls when <see cref="Labeling"/> instances are created.
        /// </summary>
        /// <param name="generator">The generator to deactivate</param>
        /// <returns>
        /// True if the <see cref="generator"/> was successfully removed.
        /// False if <see cref="generator"/> was not active.
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
            var terrain = gameObject.GetComponent<Terrain>();
            if (terrain != null)
            {
                terrain.GetSplatMaterialPropertyBlock(mpb);
                foreach (var pass in m_ActiveGenerators)
                {
                    if (labeling.enabled)
                    {
                        pass.SetupMaterialProperties(mpb, null, labeling, instanceId);
                    }
                    else
                    {
                        pass.ClearMaterialProperties(mpb, null, labeling, instanceId);
                    }
                }

                terrain.SetSplatMaterialPropertyBlock(mpb);
            }

            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.GetPropertyBlock(mpb);
                foreach (var pass in m_ActiveGenerators)
                {
                    if (labeling.enabled)
                    {
                        pass.SetupMaterialProperties(mpb, renderer, labeling, instanceId);
                    }
                    else
                    {
                        pass.ClearMaterialProperties(mpb, renderer, labeling, instanceId);
                    }
                }

                renderer.SetPropertyBlock(mpb);

                var materialCount = renderer.materials.Length;
                for (var i = 0; i < materialCount; i++)
                {
                    renderer.GetPropertyBlock(mpb, i);
                    //Only apply to individual materials if there is already a MaterialPropertyBlock on it
                    if (!mpb.isEmpty)
                    {
                        foreach (var pass in m_ActiveGenerators)
                        {
                            if (labeling.enabled)
                            {
                                pass.SetupMaterialProperties(mpb, renderer, labeling, instanceId);
                            }
                            else
                            {
                                pass.ClearMaterialProperties(mpb, renderer, labeling, instanceId);
                            }
                        }

                        renderer.SetPropertyBlock(mpb, i);
                    }
                }
            }

            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                if (child.GetComponent<Labeling>() != null)
                    continue;

                RecursivelyInitializeGameObjects(child, mpb, labeling, instanceId);
            }
        }
    }
}
