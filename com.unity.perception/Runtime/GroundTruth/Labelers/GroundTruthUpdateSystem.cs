using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// System which notifies the registered <see cref="IGroundTruthUpdater"/> about ground truth updates.
    /// </summary>
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

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            //These are here to inform the system runner the queries we are interested in. Without these calls, OnUpdate() might not be called
            GetEntityQuery(ComponentType.ReadOnly<Labeling>());
            m_QueryBuilder = Entities.WithAllReadOnly<Labeling, GroundTruthInfo>();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            foreach (var updater in m_ActiveUpdaters)
            {
                updater.OnBeginUpdate();
                m_QueryBuilder.ForEach((Entity entity, Labeling labeling, ref GroundTruthInfo groundTruth) =>
                {
                    updater.OnUpdateEntity(labeling, groundTruth);
                });
                updater.OnEndUpdate();
            }
        }
    }
}
