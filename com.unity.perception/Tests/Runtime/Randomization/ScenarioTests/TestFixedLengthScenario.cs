using System;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Scenarios;

namespace RandomizationTests.ScenarioTests
{
    [AddComponentMenu("")]
    class TestFixedLengthScenario : FixedLengthScenario
    {
        protected override void OnComplete()
        {
            DatasetCapture.ResetSimulation();
        }
    }
}
