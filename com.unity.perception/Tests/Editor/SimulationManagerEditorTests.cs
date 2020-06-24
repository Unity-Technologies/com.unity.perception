using System;
using NUnit.Framework;
using UnityEngine.Perception.GroundTruth;

namespace GroundTruth
{
    public class DatasetCaptureEditorTests
    {
        [Test]
        public void RegisterEgo_InEditMode_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => DatasetCapture.RegisterEgo(""));
        }
        [Test]
        public void RegisterAnnotationDefinition_InEditMode_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => DatasetCapture.RegisterAnnotationDefinition(""));
        }
        [Test]
        public void RegisterMetricDefinition_InEditMode_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => DatasetCapture.RegisterMetricDefinition(""));
        }
    }
}
