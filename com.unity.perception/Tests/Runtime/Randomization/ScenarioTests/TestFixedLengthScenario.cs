using System;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Scenarios;

namespace RandomizationTests.ScenarioTests
{
    class TestFixedLengthScenario : FixedLengthScenario
    {
        protected override void OnComplete()
        {
            DatasetCapture.ResetSimulation();
        }
    }
}
