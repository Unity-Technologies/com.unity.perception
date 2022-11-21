using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class CameraSensorTests : GroundTruthTestBase
    {
        [UnityTest]
        public IEnumerator ChannelCannotBeEnabledAfterSensorBeginsRendering()
        {
            var camera = SetupCamera();
            yield return null;
            Assert.Throws<InvalidOperationException>(() =>
            {
                camera.EnableChannel<InstanceIdChannel>();
            });
        }

        [Test]
        public void ChannelCannotBeAccessedBeforeItHasBeenEnabled()
        {
            var camera = SetupCamera();
            Assert.Throws<InvalidOperationException>(() =>
            {
                camera.GetChannel<InstanceIdChannel>();
            });
        }

        PerceptionCamera SetupCamera()
        {
            var cameraObject = new GameObject("Camera");
            AddTestObjectForCleanup(cameraObject);

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60;
            camera.orthographic = false;

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            return perceptionCamera;
        }
    }
}
