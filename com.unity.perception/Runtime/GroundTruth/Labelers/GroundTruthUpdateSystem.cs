using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace UnityEngine.Perception.GroundTruth
{
    public class GroundTruthUpdateSystem : ComponentSystem
    {
        List<IGroundTruthUpdater> m_ActiveUpdaters = new List<IGroundTruthUpdater>();

        public void Activate(IGroundTruthUpdater updater)
        {
            m_ActiveUpdaters.Add(updater);
        }

        public void Deactivate(IGroundTruthUpdater updater)
        {
            m_ActiveUpdaters.Remove(updater);
        }

        EntityQueryBuilder m_QueryBuilder;
        EntityQuery m_Query;

        protected override void OnCreate()
        {
            //These are here to inform the system runner the queries we are interested in. Without these calls, OnUpdate() might not be called
            GetEntityQuery(ComponentType.ReadOnly<Labeling>());

            m_QueryBuilder = Entities.WithAllReadOnly<Labeling, GroundTruthInfo>();
            m_Query = m_QueryBuilder.ToEntityQuery();
        }

        protected override void OnUpdate()
        {
            var count = m_Query.CalculateEntityCount();

            foreach (var updater in m_ActiveUpdaters)
            {
                updater.OnBeginUpdate(count);
                m_QueryBuilder.ForEach((Entity entity, Labeling labeling, ref GroundTruthInfo groundTruth) =>
                {
                    updater.OnUpdateEntity(labeling, groundTruth);
                });
                updater.OnEndUpdate();
            }
        }
    }
}
