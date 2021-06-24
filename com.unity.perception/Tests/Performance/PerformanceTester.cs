using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PerformanceTests
{
    public abstract class PerformanceTester
    {
        (int, int) m_Resolution;
        bool m_CaptureData;
        bool m_VisualizersOn;
        PerceptionCamera m_Camera;
        GameObject m_SceneRoot;
        IdLabelConfig m_IdConfig = null;
        SemanticSegmentationLabelConfig m_SsConfig = null;
        string[] m_Labelers = null;
        List<CameraLabeler> m_ActiveLabelers = null;

        public PerformanceTester(int resx ,int resy, bool capData, bool vizOn, string labeler1)
        {
            this.m_Resolution = (resx, resy);
            this.m_CaptureData = capData;
            this.m_VisualizersOn = vizOn;
            this.m_Labelers = new string[]{ labeler1 };
        }

        public PerformanceTester(int resx ,int resy, bool capData, bool vizOn, string labeler1, string labeler2)
        {
            this.m_Resolution = (resx, resy);
            this.m_CaptureData = capData;
            this.m_VisualizersOn = vizOn;
            this.m_Labelers = new string[]{ labeler1, labeler2 };
        }

        public PerformanceTester(int resx ,int resy, bool capData, bool vizOn, string labeler1, string labeler2, string labeler3)
        {
            this.m_Resolution = (resx, resy);
            this.m_CaptureData = capData;
            this.m_VisualizersOn = vizOn;
            this.m_Labelers = new string[]{ labeler1, labeler2, labeler3 };
        }

        public PerformanceTester(int resx ,int resy, bool capData, bool vizOn, string labeler1, string labeler2, string labeler3, string labeler4)
        {
            this.m_Resolution = (resx, resy);
            this.m_CaptureData = capData;
            this.m_VisualizersOn = vizOn;
            this.m_Labelers = new string[]{ labeler1, labeler2, labeler3, labeler4 };
        }

        private static CameraLabeler CreateLabeler(string label, IdLabelConfig idConfig, SemanticSegmentationLabelConfig ssConfig)
        {
            switch (label) {
                case PerformanceTestObjectCountLabeler.Label:
                    return new ObjectCountLabeler(idConfig);
                case PerformanceTestBoundingBoxLabeler.Label:
                    return new BoundingBox2DLabeler(idConfig);
                case PerformanceTestPixelCountLabeler.Label:
                     return new RenderedObjectInfoLabeler(idConfig);
                case PerformanceTestSemanticSegmentationLabeler.Label:
                     return new SemanticSegmentationLabeler(ssConfig);

                default:
                    return null;
            }
        }

        private static List<CameraLabeler> CreateLabelers(IEnumerable<string> labelers, IdLabelConfig idConfig, SemanticSegmentationLabelConfig ssConfig)
        {
            return labelers.Select(l => CreateLabeler(l, idConfig, ssConfig)).Where(labeler => labeler != null).ToList();
        }

        [SetUp]
        public void SetUpTest()
        {
            DatasetCapture.ResetSimulation();
            Time.timeScale = 1;

            if (Directory.Exists(DatasetCapture.OutputDirectory))
                Directory.Delete(DatasetCapture.OutputDirectory, true);

            Screen.SetResolution(m_Resolution.Item1, m_Resolution.Item2, true);
            (m_Camera, m_IdConfig, m_SsConfig, m_SceneRoot) = TestHelper.CreateThreeBlockScene();

            m_Camera.enabled = true;
            m_Camera.showVisualizations = false;

            m_ActiveLabelers = CreateLabelers(m_Labelers, m_IdConfig, m_SsConfig);

            foreach (var l in m_ActiveLabelers)
            {
                m_Camera.AddLabeler(l);
            }

            if (!m_CaptureData) m_Camera.enabled = false;
            if (m_CaptureData && !m_VisualizersOn) m_Camera.showVisualizations = false;
            m_Camera.gameObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Camera.gameObject);
            Object.DestroyImmediate(m_SceneRoot.gameObject);

            var simState = DatasetCapture.SimulationState;
            simState.End();

            DatasetCapture.ResetSimulation();
            Time.timeScale = 1;
            if (Directory.Exists(DatasetCapture.OutputDirectory))
                Directory.Delete(DatasetCapture.OutputDirectory, true);

            m_ActiveLabelers = null;
            m_IdConfig = null;
            m_SsConfig = null;
        }

        [UnityTest, Performance]
        public IEnumerator ExecuteTest()
        {
            yield return Measure.Frames()
                .WarmupCount(10)
                .MeasurementCount(30)
                .Run();

            // Allow all file writes to complete
            yield return new WaitForSeconds(5);
        }
    }
}
