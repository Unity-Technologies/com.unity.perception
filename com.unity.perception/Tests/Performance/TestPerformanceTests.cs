using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PerformanceTests
{
    [TestFixture(640, 480, false, false)]
    [TestFixture(640, 480, true, false)]
    [TestFixture(640, 480, true, true)]
    [TestFixture(1024, 768, false, false)]
    [TestFixture(1024, 768, true, false)]
    [TestFixture(1024, 768, true, true)]
    [TestFixture(1920, 1080, false, false)]
    [TestFixture(1920, 1080, true, false)]
    [TestFixture(1920, 1080, true, true)]
    [Category("Performance")]
    public class TestPerformanceTestsTwo
    {
        (int, int) m_Resolution;
        bool m_CaptureData;
        bool m_VisualizersOn;
        PerceptionCamera m_Camera;
        GameObject m_SceneRoot;
        IdLabelConfig m_Config = null;
        CameraLabeler m_ActiveLabeler = null;

        public TestPerformanceTestsTwo(int resx ,int resy, bool capData, bool vizOn)
        {
            this.m_Resolution = (resx, resy);
            this.m_CaptureData = capData;
            this.m_VisualizersOn = vizOn;
        }

        [SetUp]
        public void SetUpTest()
        {
            Screen.SetResolution(m_Resolution.Item1, m_Resolution.Item2, true);
            (m_Camera, m_Config, m_SceneRoot) = TestHelper.CreateThreeBlockScene();
            m_ActiveLabeler = new ObjectCountLabeler(m_Config);
            m_Camera.AddLabeler(m_ActiveLabeler);
            if (!m_CaptureData) m_Camera.enabled = false;
            if (m_CaptureData && !m_VisualizersOn) m_Camera.showVisualizations = false;
            m_Camera.gameObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Camera.gameObject);
            Object.DestroyImmediate(m_SceneRoot.gameObject);

            DatasetCapture.ResetSimulation();
            Time.timeScale = 1;

            if (Directory.Exists(DatasetCapture.OutputDirectory))
                Directory.Delete(DatasetCapture.OutputDirectory, true);

            m_ActiveLabeler = null;
            m_Config = null;

        }
#if true
        [UnityTest, Performance]
        public IEnumerator ExecuteTest()
        {
            yield return Measure.Frames()
                .WarmupCount(0)
                .MeasurementCount(20)
                .Run();

            // Allow all file writes to complete
            yield return new WaitForSeconds(5);
        }
#endif
    }


    #if false
    public class TestPerformanceTests
    {
        [UnityTest, Performance]
        public IEnumerator PerceptionCamera_NoPerception()
        {
            yield return ExecuteTest((640, 480), false, false);
        }

        [Test, Performance]
        public void PerceptionCamera_PerceptionNoVisuals()
        {
            ExecuteTest((640, 480), true, false);
        }

        [Test, Performance]
        public void PerceptionCamera_PerceptionWithVisuals()
        {
            ExecuteTest((640, 480), true, true);
        }

        [SetUp]
        private void SetUp()
        {
            Screen.SetResolution(imageSize.Item1, imageSize.Item2, true);
            var (cam, config) = TestHelper.CreateThreeBlockScene();
            var ocLabeler = new ObjectCountLabeler(config);
            cam.AddLabeler(ocLabeler);
            if (!captureData) cam.enabled = false;
            if (captureData && !visualizersOn) cam.showVisualizations = false;
            cam.gameObject.SetActive(true);
        }

        private IEnumerator ExecuteTest((int, int) imageSize, bool captureData = true, bool visualizersOn = true)
        {
            yield return Measure.Frames(() =>
            {
                Screen.SetResolution(imageSize.Item1, imageSize.Item2, true);
                var (cam, config) = TestHelper.CreateThreeBlockScene();
                var ocLabeler = new ObjectCountLabeler(config);
                cam.AddLabeler(ocLabeler);
                if (!captureData) cam.enabled = false;
                if (captureData && !visualizersOn) cam.showVisualizations = false;
                cam.gameObject.SetActive(true);
            })
                .WarmupCount(5)
                .MeasurementCount(20)
                .Run();
        }

        [Test, Performance]
        public void Vector2_operations()
        {
            var a = Vector2.one;
            var b = Vector2.zero;

            Measure.Method(() =>
            {
                Vector2.MoveTowards(a, b, 0.5f);
                Vector2.ClampMagnitude(a, 0.5f);
                Vector2.Reflect(a, b);
                Vector2.SignedAngle(a, b);

            }).MeasurementCount(20).WarmupCount(5).IterationsPerMeasurement(1000000).Run();
        }


        // A Test behaves as an ordinary method
        [Test, Performance]
        public void TestPerformanceTestsSimplePasses()
        {

            var a = Vector2.one;
            var b = Vector2.zero;

            Measure.Method(() =>
            {
                Vector2.MoveTowards(a, b, 0.5f);
                Vector2.ClampMagnitude(a, 0.5f);
                Vector2.Reflect(a, b);
                Vector2.SignedAngle(a, b);

            }).MeasurementCount(50).WarmupCount(5).IterationsPerMeasurement(10).Run();

            // Use the Assert class to test conditions
        }
    }
#endif
}
