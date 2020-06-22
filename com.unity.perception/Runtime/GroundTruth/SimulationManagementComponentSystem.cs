using System;
using Unity.Entities;

namespace UnityEngine.Perception.GroundTruth
{
    class SimulationManagementComponentSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            DatasetCapture.SimulationState?.Update();
        }
    }
}
