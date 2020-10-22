using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

namespace UnityEngine.Perception.GroundTruth
{
    public class InstanceSegmentationLabeler : CameraLabeler
    {
        public override string description
        {
            get => "";
            protected set { }
        }

        protected override bool supportsVisualization => false;

        const string k_Directory = "InstanceSegmentation";
        const string k_FilePrefix = "Instance_";

        public SemanticSegmentationLabelConfig labelConfig;

        [Tooltip("The id to associate with instance segmentation annotations in the dataset.")]
        public string annotationId = "1ccebeb4-5886-41ff-8fe0-f911fa8cbcdf";

        AnnotationDefinition m_AnnotationDefinition;

        public InstanceSegmentationLabeler() { }

        public InstanceSegmentationLabeler(SemanticSegmentationLabelConfig labelConfig)
        {
            this.labelConfig = labelConfig;

        }

        Dictionary<int, AsyncAnnotation> m_AsyncAnnotations;

        void OnImageCaptured(int frame, NativeArray<uint> data, RenderTexture renderTexture)
        {
            if (!m_AsyncAnnotations.TryGetValue(frame, out var annotation))
                return;

            var path = $"{k_Directory}/{k_FilePrefix}{frame}.png";
            var localPath = $"{Manager.Instance.GetDirectoryFor(k_Directory)}/{k_FilePrefix}{frame}.png";

            annotation.ReportFile(path);

            var colors = new NativeArray<Color32>(data.Length, Allocator.TempJob);

            for (var i = 0; i < data.Length; i++)
            {
                if (data[i] == 0) colors[i] = Color.black;
                else if (data[i] == 1) colors[i] = Color.red;
                else if (data[i] == 2) colors[i] = Color.green;
                else if (data[i] == 3) colors[i] = Color.blue;
                else colors[i] = Color.gray;
            }

            var asyncRequest = Manager.Instance.CreateRequest<AsyncRequest<AsyncWrite>>();

            asyncRequest.data = new AsyncWrite
            {
                data = colors,
                width = renderTexture.width,
                height = renderTexture.height,
                path = localPath
            };

            asyncRequest.Enqueue(r =>
            {
                Profiler.BeginSample("Encode");
                var pngBytes = ImageConversion.EncodeArrayToPNG(r.data.data.ToArray(), GraphicsFormat.R8G8B8A8_UNorm, (uint)r.data.width, (uint)r.data.height);
                Profiler.EndSample();
                Profiler.BeginSample("WritePng");
                File.WriteAllBytes(r.data.path, pngBytes);
                Manager.Instance.ConsumerFileProduced(r.data.path);
                Profiler.EndSample();
                r.data.data.Dispose();
                return AsyncRequest.Result.Completed;
            });
            asyncRequest.Execute();
        }

        protected override void OnBeginRendering()
        {
            m_AsyncAnnotations[Time.frameCount] = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition);
        }

        struct AsyncWrite
        {
            public NativeArray<Color32> data;
            public int width;
            public int height;
            public string path;
        }

        struct Spec
        {
            public string label_name;
            public Color pixel_value;
        }

        int m_CamWidth = 0;
        int m_CamHeight = 0;

        protected override void Setup()
        {
            var myCamera = perceptionCamera.GetComponent<Camera>();
            m_CamWidth = myCamera.pixelWidth;
            m_CamHeight = myCamera.pixelHeight;

            if (labelConfig == null)
            {
                throw new InvalidOperationException(
                    "InstanceSegmentationLabeler's LabelConfig must be assigned");
            }

            perceptionCamera.InstanceSegmentationImageReadback += OnImageCaptured;

            m_AsyncAnnotations = new Dictionary<int, AsyncAnnotation>();

            var specs = labelConfig.labelEntries.Select(l => new Spec
            {
                label_name = l.label,
                pixel_value = l.color
            }).ToArray();

            m_AnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition(
                "instance segmentation",
                specs,
                "pixel-wise instance segmentation label",
                "PNG",
                Guid.Parse(annotationId));

            visualizationEnabled = supportsVisualization;
        }
    }



}
