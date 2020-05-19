using System;
using NUnit.Framework;
using UnityEngine.Perception.GroundTruth;

namespace GroundTruth
{
    public class SimulationManagerEditorTests
    {
        [Test]
        public void RegisterEgo_InEditMode_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => SimulationManager.RegisterEgo(""));
        }
        [Test]
        public void RegisterAnnotationDefinition_InEditMode_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => SimulationManager.RegisterAnnotationDefinition(""));
        }
        [Test]
        public void RegisterMetricDefinition_InEditMode_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => SimulationManager.RegisterMetricDefinition(""));
        }
    }
}
