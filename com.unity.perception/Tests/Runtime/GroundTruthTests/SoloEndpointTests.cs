using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.Settings;

namespace GroundTruthTests
{
    [TestFixture]
    public class SoloEndpointTests
    {
        [Test]
        public void TestWritingCaptureFiles_WithData()
        {
            DatasetCapture.ResetSimulation();

            var endpoint = new SoloEndpoint();

            endpoint.basePath = PerceptionSettings.defaultOutputPath;
            endpoint.soloDatasetName = Guid.NewGuid().ToString();

            var frame = new Frame(0, 0, 0, 0);
            var sensor = new RgbSensor(new RgbSensorDefinition("camera", "camera", "camera"), Vector3.zero, Quaternion.identity);

            var texture = new Texture2D(3, 3, TextureFormat.RGB24, false);
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    texture.SetPixel(x, y, Color.blue);
                }
            }
            texture.Apply();

            sensor.buffer = texture.EncodeToPNG();
            frame.sensors.Add(sensor);
            endpoint.FrameGenerated(frame);

            var cp = endpoint.currentPath;

            // verify that image file exists
            var p = PathUtils.CombineUniversal(cp, "sequence.0", "step0.camera.png");
            FileAssert.Exists(p);

            p = PathUtils.CombineUniversal(cp, "sequence.0", "step0.frame_data.json");

            FileAssert.Exists(p);
            var jsonActual = File.ReadAllText(p);
            Assert.IsTrue(jsonActual.Contains("\"filename\": \"step0.camera.png\""));

            Directory.Delete(cp, true);
        }

        [Test]
        public void TestWritingCaptureFiles_NoData()
        {
            DatasetCapture.ResetSimulation();

            var endpoint = new SoloEndpoint();
            endpoint.basePath = PerceptionSettings.defaultOutputPath;
            endpoint.soloDatasetName = Guid.NewGuid().ToString();

            var frame = new Frame(0, 0, 0, 0);
            var sensor = new RgbSensor(new RgbSensorDefinition("camera", "camera", "camera"), Vector3.zero, Quaternion.identity);

            sensor.buffer = Array.Empty<byte>();
            frame.sensors.Add(sensor);
            endpoint.FrameGenerated(frame);

            var cp = endpoint.currentPath;

            // verify that image file exists
            var p = PathUtils.CombineUniversal(cp, "sequence.0", "step0.camera.png");
            FileAssert.DoesNotExist(p);

            p = PathUtils.CombineUniversal(cp, "sequence.0", "step0.frame_data.json");

            FileAssert.Exists(p);
            var jsonActual = File.ReadAllText(p);
            Assert.IsTrue(jsonActual.Contains("\"filename\": null"));

            Directory.Delete(cp, true);
        }
    }
}
