using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    [Serializable]
    public class DatasetCaptureEditorTests
    {
        [SerializeField]
        string expectedDatasetPath;
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
        [UnityTest]
        public IEnumerator SimpleData_GeneratesFullDataset_OnExitPlaymode()
        {
            yield return new EnterPlayMode();
            DatasetCapture.ResetSimulation();
            var ego = DatasetCapture.RegisterEgo("ego");
            var sensor = DatasetCapture.RegisterSensor(ego, "camera", "", 0.1f, 0);
            sensor.ReportCapture("file.txt", new SensorSpatialData());
            expectedDatasetPath = DatasetCapture.OutputDirectory;
            yield return new ExitPlayMode();
            FileAssert.Exists(Path.Combine(expectedDatasetPath, "sensors.json"));
        }
    }
}
