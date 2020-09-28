using System;

namespace UnityEngine.Experimental.Perception.Randomization.Scenarios
{
    public abstract class USimScenario<T> : Scenario<T> where T : USimConstants, new()
    {
        public override bool isScenarioComplete => currentIteration >= constants.totalIterations;

        protected override void OnAwake()
        {
            currentIteration = constants.instanceIndex;
        }

        protected override void IncrementIteration()
        {
            currentIteration += constants.instanceCount;
        }

        public override void Deserialize()
        {
            if (string.IsNullOrEmpty(Unity.Simulation.Configuration.Instance.SimulationConfig.app_param_uri))
                base.Deserialize();
            else
                constants = Unity.Simulation.Configuration.Instance.GetAppParams<T>();
        }
    }

    [Serializable]
    public class USimConstants
    {
        public int totalIterations = 100;
        public int instanceCount = 1;
        public int instanceIndex;
    }
}
