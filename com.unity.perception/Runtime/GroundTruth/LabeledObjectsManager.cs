using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Randomizers;

namespace UnityEngine.Perception.GroundTruth
{
    class LabeledObjectsManager
    {
        public static LabeledObjectsManager singleton { get; } = new LabeledObjectsManager();

        const uint k_StartingIndex = 1;
        uint m_CurrentObjectIndex = k_StartingIndex;
        List<IGroundTruthGenerator> m_ActiveGenerators = new List<IGroundTruthGenerator>();
        LinkedHashSet<Labeling> m_UnregisteredLabels = new LinkedHashSet<Labeling>();
        LinkedHashSet<Labeling> m_RegisteredLabels = new LinkedHashSet<Labeling>();

        public IEnumerable<Labeling> registeredLabels => m_RegisteredLabels;

        public void Update()
        {
            if (m_RegisteredLabels.Count == 0)
                m_CurrentObjectIndex = k_StartingIndex;

            foreach (var unregisteredLabel in m_UnregisteredLabels)
            {
                if (m_RegisteredLabels.Contains(unregisteredLabel))
                    continue;

                var instanceId = m_CurrentObjectIndex++;
                InitGameObjectRecursive(
                    unregisteredLabel.gameObject,
                    new MaterialPropertyBlock(),
                    unregisteredLabel,
                    instanceId);

                unregisteredLabel.SetInstanceId(instanceId);
                m_RegisteredLabels.Add(unregisteredLabel);
            }

            m_UnregisteredLabels.Clear();
        }

        /// <summary>
        /// Activates the given <see cref="IGroundTruthGenerator"/>. <see cref="IGroundTruthGenerator.SetupMaterialProperties"/>
        /// will be called for all <see cref="MeshRenderer"/> instances under each object containing a <see cref="Labeling"/> component.
        /// </summary>
        /// <param name="generator">The generator to register</param>
        public void Activate(IGroundTruthGenerator generator)
        {
            m_ActiveGenerators.Add(generator);
            foreach (var label in m_RegisteredLabels)
                InitGameObjectRecursive(label.gameObject, new MaterialPropertyBlock(), label, label.instanceId);
        }

        /// <summary>
        /// Deactivates the given <see cref="IGroundTruthGenerator"/>. It will no longer receive calls when <see cref="Labeling"/> instances are created.
        /// </summary>
        /// <param name="generator">The generator to deactivate</param>
        /// <returns>True if the <see cref="generator"/> was successfully removed. False if <see cref="generator"/> was not active.</returns>
        public bool Deactivate(IGroundTruthGenerator generator)
        {
            return m_ActiveGenerators.Remove(generator);
        }

        /// <summary>
        /// Registers a labeling component
        /// </summary>
        /// <param name="labeledObject">the component to register</param>
        public void Register(Labeling labeledObject)
        {
            m_UnregisteredLabels.Add(labeledObject);
        }

        /// <summary>
        /// Unregisters a labeling component
        /// </summary>
        /// <param name="labeledObject">the component to unregister</param>
        public void Unregister(Labeling labeledObject)
        {
            m_UnregisteredLabels.Remove(labeledObject);
            m_RegisteredLabels.Remove(labeledObject);
        }

        /// <summary>
        /// Refresh ground truth generation for the labeling of a particular GameObject. This is necessary when the
        /// list of labels changes or when renderers or materials change on objects in the hierarchy.
        /// </summary>
        /// <param name="labeledObject">the component to refresh</param>
        public void RefreshLabeling(Labeling labeledObject)
        {
            m_RegisteredLabels.Remove(labeledObject);
            m_UnregisteredLabels.Add(labeledObject);
        }

        void InitGameObjectRecursive(
            GameObject gameObject, MaterialPropertyBlock mpb, Labeling labeling, uint instanceId)
        {
            var terrain = gameObject.GetComponent<Terrain>();
            if (terrain != null)
            {
                terrain.GetSplatMaterialPropertyBlock(mpb);
                foreach (var pass in m_ActiveGenerators)
                    pass.SetupMaterialProperties(mpb, null, labeling, instanceId);

                terrain.SetSplatMaterialPropertyBlock(mpb);
            }

            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.GetPropertyBlock(mpb);
                foreach (var pass in m_ActiveGenerators)
                    pass.SetupMaterialProperties(mpb, renderer, labeling, instanceId);

                renderer.SetPropertyBlock(mpb);

                var materialCount = renderer.materials.Length;
                for (var i = 0; i < materialCount; i++)
                {
                    renderer.GetPropertyBlock(mpb, i);
                    //Only apply to individual materials if there is already a MaterialPropertyBlock on it
                    if (!mpb.isEmpty)
                    {
                        foreach (var pass in m_ActiveGenerators)
                            pass.SetupMaterialProperties(mpb, renderer, labeling, instanceId);

                        renderer.SetPropertyBlock(mpb, i);
                    }
                }
            }

            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                if (child.GetComponent<Labeling>() != null)
                    continue;

                InitGameObjectRecursive(child, mpb, labeling, instanceId);
            }
        }
    }
}
