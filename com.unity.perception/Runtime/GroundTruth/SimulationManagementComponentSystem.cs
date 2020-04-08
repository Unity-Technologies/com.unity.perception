using System;
using Unity.Entities;

namespace UnityEngine.Perception
{
    class SimulationManagementComponentSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            SimulationManager.SimulationState?.Update();
        }
    }
}
