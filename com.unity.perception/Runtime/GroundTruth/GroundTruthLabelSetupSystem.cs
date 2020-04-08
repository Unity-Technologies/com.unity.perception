using System.Collections.Generic;
using System.Threading;
using Unity.Entities;

namespace UnityEngine.Perception.Sensors
{
    struct IdAssignmentParameters : IComponentData
    {
        public uint idStart;
        public uint idStep;
    }
    public class GroundTruthLabelSetupSystem : ComponentSystem
    {
        List<IGroundTruthGenerator> m_ActiveGenerators = new List<IGroundTruthGenerator>();
        ThreadLocal<MaterialPropertyBlock> m_MaterialPropertyBlocks = new ThreadLocal<MaterialPropertyBlock>();
        int m_CurrentObjectIndex = -1;

        protected override void OnCreate()
        {
            //These are here to inform the system runner the queries we are interested in. Without these calls, OnUpdate() might not be called
            GetEntityQuery( ComponentType.Exclude<GroundTruthInfo>(), ComponentType.ReadOnly<Labeling>());
            GetEntityQuery( ComponentType.ReadOnly<GroundTruthInfo>(), ComponentType.ReadOnly<Labeling>());
        }

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
            });
        }

        void InitGameObjectRecursive(GameObject gameObject, MaterialPropertyBlock mpb, Labeling labeling, uint instanceId)
        {
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.GetPropertyBlock(mpb);
                foreach (var pass in m_ActiveGenerators)
                    pass.SetupMaterialProperties(mpb, meshRenderer, labeling, instanceId);

                meshRenderer.SetPropertyBlock(mpb);
            }

            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                if (child.GetComponent<Labeling>() != null)
                    continue;

                InitGameObjectRecursive(child, mpb, labeling, instanceId);
            }
        }

        public void Activate(IGroundTruthGenerator pass)
        {
            m_ActiveGenerators.Add(pass);
            Entities.ForEach((Labeling labeling, ref GroundTruthInfo info) =>
            {
                var gameObject = labeling.gameObject;
                InitGameObjectRecursive(gameObject, m_MaterialPropertyBlocks.Value, labeling, info.instanceId);
            });

        }

        public void Deactivate(IGroundTruthGenerator pass)
        {
            m_ActiveGenerators.Remove(pass);
        }
    }
}
