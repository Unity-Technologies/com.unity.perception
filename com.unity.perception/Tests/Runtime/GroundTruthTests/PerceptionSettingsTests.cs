using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.Settings;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class PerceptionSettingsTests
    {
        [UnityTest]
        public IEnumerator TestSetEndpoint_CollectEndpoint()
        {
            const string id = "camera";
            const string modality = "camera";
            const string def = "Cam (FL2-14S3M-C)";
            const int firstFrame = 1;
            const CaptureTriggerMode mode = CaptureTriggerMode.Scheduled;
            const int delta = 1;
            const int framesBetween = 0;

            PerceptionSettings.endpoint = new CollectEndpoint();
            DatasetCapture.ResetSimulation();

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor(id, modality, def, firstFrame, mode, delta, framesBetween);
            Assert.IsTrue(sensorHandle.IsValid);

            yield return null;

            // grab a handle for the endpoint of the current run
            var collector = (CollectEndpoint)DatasetCapture.activateEndpoint;

            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            // Check metadata
            var meta = collector.currentRun.metadata as SimulationMetadata;
            Assert.NotNull(meta);
            Assert.AreEqual(meta.perceptionVersion, DatasetCapture.perceptionVersion);
            Assert.AreEqual(meta.unityVersion, Application.unityVersion);

            // Check sensor data
            Assert.AreEqual(collector.sensors.Count, 1);
            var sensor = collector.sensors.First();
            Assert.NotNull(sensor);
            Assert.AreEqual(sensor.id, id);
            Assert.AreEqual(sensor.modality, modality);
            Assert.AreEqual(sensor.description, def);
            Assert.AreEqual(sensor.firstCaptureFrame, firstFrame);
            Assert.AreEqual(sensor.captureTriggerMode, mode);
            Assert.AreEqual(sensor.simulationDeltaTime, delta);
            Assert.AreEqual(sensor.framesBetweenCaptures, framesBetween);
        }

#if UNITY_EDITOR
        // Perception only supports setting the output directory from the command line in non editor based simulations
        [UnityTest]
        public IEnumerator TestSetOutputDirectory()
        {
            const string id = "camera";
            const string modality = "camera";
            const string def = "Cam (FL2-14S3M-C)";
            const int firstFrame = 1;
            const CaptureTriggerMode mode = CaptureTriggerMode.Scheduled;
            const int delta = 1;
            const int framesBetween = 0;

            PerceptionSettings.endpoint = new PerceptionEndpoint();

            var savePath = PerceptionSettings.GetOutputBasePath();

            var outputPath = Path.Combine(PerceptionSettings.defaultOutputPath, $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(outputPath);
            PerceptionSettings.SetOutputBasePath(outputPath);

            DatasetCapture.ResetSimulation();

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor(id, modality, def, firstFrame, mode, delta, framesBetween);
            Assert.IsTrue(sensorHandle.IsValid);

            yield return null;

            // grab a handle for the endpoint of the current run
            var endpoint = (PerceptionEndpoint)DatasetCapture.activateEndpoint;

            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            var path = endpoint.datasetPath;

            // Verify that the output directory exists and a captures and a metrics file is written to it
            Assert.IsTrue(path.Contains(outputPath));
            DirectoryAssert.Exists(path);
            FileAssert.Exists(Path.Combine(path, "captures_000.json"));
            FileAssert.Exists(Path.Combine(path, "metrics_000.json"));

            Directory.Delete(outputPath, true);

            DirectoryAssert.DoesNotExist(outputPath);

            PerceptionSettings.SetOutputBasePath(savePath);
        }

#endif
    }
}
