using System.Collections.Generic;
using System.Threading;
using Unity.Entities;

namespace UnityEngine.Perception.GroundTruth
{
    struct IdAssignmentParameters : IComponentData
    {
        public uint idStart;
        public uint idStep;
    }
    /// <summary>
    /// System which notifies the registered <see cref="IGroundTruthGenerator"/> about <see cref="Labeling"/> additions.
    /// </summary>
    public class GroundTruthLabelSetupSystem : ComponentSystem
    {
        List<IGroundTruthGenerator> m_ActiveGenerators = new List<IGroundTruthGenerator>();
        ThreadLocal<MaterialPropertyBlock> m_MaterialPropertyBlocks = new ThreadLocal<MaterialPropertyBlock>();
        int m_CurrentObjectIndex = -1;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            //These are here to inform the system runner the queries we are interested in. Without these calls, OnUpdate() might not be called
            GetEntityQuery(ComponentType.Exclude<GroundTruthInfo>(), ComponentType.ReadOnly<Labeling>());
            GetEntityQuery(ComponentType.ReadOnly<GroundTruthInfo>(), ComponentType.ReadOnly<Labeling>());
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var entityQuery = Entities.WithAll<IdAssignmentParameters>().ToEntityQuery();
            IdAssignmentParameters idAssignmentParameters;
            if (entityQuery.CalculateEntityCount() == 1)
                idAssignmentParameters = entityQuery.GetSingleton<IdAssignmentParameters>();
            else
                idAssignmentParameters = new IdAssignmentParameters {idStart = 1, idStep = 1};

            var entityCount = Entities.WithAll<Labeling, GroundTruthInfo>().ToEntityQuery().CalculateEntityCount();
            if (entityCount == 0)
                m_CurrentObjectIndex = -1;

            Entities.WithNone<GroundTruthInfo>().ForEach((Entity e, Labeling labeling) =>
            {
                var objectIndex = (uint)Interlocked.Increment(ref m_CurrentObjectIndex);
                var instanceId = idAssignmentParameters.idStart + objectIndex * idAssignmentParameters.idStep;
                var gameObject = labeling.gameObject;
                if (!m_MaterialPropertyBlocks.IsValueCreated)
                    m_MaterialPropertyBlocks.Value = new MaterialPropertyBlock();

                InitGameObjectRecursive(gameObject, m_MaterialPropertyBlocks.Value, labeling, instanceId);
                EntityManager.AddComponentData(e, new GroundTruthInfo
                {
                    instanceId = instanceId
                });
                labeling.SetInstanceId(instanceId);
            });
        }

        void InitGameObjectRecursive(GameObject gameObject, MaterialPropertyBlock mpb, Labeling labeling, uint instanceId)
        {

            var terrain = gameObject.GetComponent<Terrain>();

            if (terrain != null)
            {
                terrain.GetSplatMaterialPropertyBlock(mpb);
                foreach (var pass in m_ActiveGenerators)
                    pass.SetupMaterialProperties(mpb, null, labeling, instanceId);

                terrain.SetSplatMaterialPropertyBlock(mpb);
            }

            var renderer = (Renderer)gameObject.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = gameObject.GetComponent<SkinnedMeshRenderer>();

            if (renderer != null)
            {
                renderer.GetPropertyBlock(mpb);
                foreach (var pass in m_ActiveGenerators)
                    pass.SetupMaterialProperties(mpb, renderer, labeling, instanceId);

                renderer.SetPropertyBlock(mpb);

                var materialCount = renderer.materials.Length;
                for (int i = 0; i < materialCount; i++)
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

        /// <summary>
        /// Activates the given <see cref="IGroundTruthGenerator"/>. <see cref="IGroundTruthGenerator.SetupMaterialProperties"/>
        /// will be called for all <see cref="MeshRenderer"/> instances under each object containing a <see cref="Labeling"/> component.
        /// </summary>
        /// <param name="generator">The generator to register</param>
        public void Activate(IGroundTruthGenerator generator)
        {
            m_ActiveGenerators.Add(generator);
            Entities.ForEach((Labeling labeling, ref GroundTruthInfo info) =>
            {
                var gameObject = labeling.gameObject;
                InitGameObjectRecursive(gameObject, m_MaterialPropertyBlocks.Value, labeling, info.instanceId);
            });
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

        internal void RefreshLabeling(Entity labelingEntity)
        {
            EntityManager.RemoveComponent<GroundTruthInfo>(labelingEntity);
        }
    }
}
