using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    ///  Produces instance segmentation for each frame.
    /// </summary>
    [Serializable]
    public sealed class InstanceSegmentationLabeler : CameraLabeler, IOverlayPanelProvider
    {
        InstanceSegmentationDefinition m_Definition;

        ///<inheritdoc/>
        public override string description => InstanceSegmentationDefinition.labelDescription;

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <summary>
        /// The GUID to associate with annotations produced by this labeler.
        /// </summary>
        [Tooltip("The id to associate with instance segmentation annotations in the dataset.")]
        public string annotationId = "instance segmentation";

        /// <inheritdoc />
        public override string labelerId => annotationId;

        /// <summary>
        /// The <see cref="idLabelConfig"/> which associates objects with labels.
        /// </summary>
        public IdLabelConfig idLabelConfig;

        /// <summary>
        /// The encoding format used when writing the captured segmentation images.
        /// </summary>
        const LosslessImageEncodingFormat k_ImageEncodingFormat = LosslessImageEncodingFormat.Png;

        static ProfilerMarker s_OnObjectInfoReceivedCallback = new ProfilerMarker("OnInstanceSegmentationObjectInformationReceived");
        static ProfilerMarker s_OnImageReceivedCallback = new ProfilerMarker("OnInstanceSegmentationImagesReceived");

        Texture m_CurrentTexture;

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        public Texture overlayImage => m_CurrentTexture;

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public string label => "InstanceSegmentation";

        Dictionary<int, AsyncFuture<DataModel.Annotation>> m_PendingFutures;
        ConcurrentDictionary<int, List<InstanceSegmentationEntry>> m_PendingEntries;
        ConcurrentDictionary<int, PendingImage> m_PendingEncodedImages;

        struct PendingImage
        {
            public int width;
            public int height;
            public byte[] data;
        }

        bool ReportFrameIfReady(int frame)
        {
            lock (m_PendingEntries)
            {
                var entriesReady = m_PendingEntries.ContainsKey(frame);
                var imgReady = m_PendingEncodedImages.ContainsKey(frame);

                if (!entriesReady || !imgReady) return false;

                if (!m_PendingEntries.TryRemove(frame, out var entries))
                {
                    throw new InvalidOperationException($"Could not remove entries for {frame} although it said it was ready");
                }

                if (!m_PendingEncodedImages.TryRemove(frame, out var img))
                {
                    throw new InvalidOperationException($"Could not remove encoded image for {frame} although it said it was ready");
                }

                var toReport = new InstanceSegmentationAnnotation(m_Definition, perceptionCamera.SensorHandle.Id, entries)
                {
                    imageFormat = ImageEncoder.ConvertFormat(k_ImageEncodingFormat),
                    dimension = new Vector2(img.width, img.height),
                    buffer = img.data
                };

                if (!m_PendingFutures.TryGetValue(frame, out var future))
                {
                    throw new InvalidOperationException($"Could not get future for {frame}");
                }

                future.Report(toReport);

                m_PendingFutures.Remove(frame);

                return true;
            }
        }

        /// <summary>
        /// Creates a new InstanceSegmentationLabeler. Be sure to assign <see cref="idLabelConfig"/> before adding to a <see cref="PerceptionCamera"/>.
        /// </summary>
        public InstanceSegmentationLabeler() { }

        /// <summary>
        /// Creates a new InstanceSegmentationLabeler with the given <see cref="IdLabelConfig"/>.
        /// </summary>
        /// <param name="labelConfig">The label config for resolving the label for each object.</param>
        public InstanceSegmentationLabeler(IdLabelConfig labelConfig)
        {
            this.idLabelConfig = labelConfig;
        }

        void OnRenderedObjectInfosCalculated(int frame, NativeArray<RenderedObjectInfo> renderedObjectInfos)
        {
            using (s_OnObjectInfoReceivedCallback.Auto())
            {
                var instances = new List<InstanceSegmentationEntry>();

                foreach (var objectInfo in renderedObjectInfos)
                {
                    if (!idLabelConfig.TryGetLabelEntryFromInstanceId(objectInfo.instanceId, out var labelEntry))
                        continue;

                    instances.Add(new InstanceSegmentationEntry
                    {
                        instanceId = (int)objectInfo.instanceId,
                        labelId = labelEntry.id,
                        labelName = labelEntry.label,
                        rgba = objectInfo.instanceColor
                    });
                }

                if (!m_PendingEntries.TryAdd(frame, instances))
                {
                    throw new InvalidOperationException($"Could not add instances for {frame}");
                }

                ReportFrameIfReady(frame);
            }
        }

        void OnImageCaptured(int frameCount, NativeArray<Color32> data, RenderTexture renderTexture)
        {
            using (s_OnImageReceivedCallback.Auto())
            {
                m_CurrentTexture = renderTexture;

                ImageEncoder.EncodeImage(data, renderTexture.width, renderTexture.height,
                    renderTexture.graphicsFormat, k_ImageEncodingFormat, encodedImageData =>
                {
                    var pendingImage = new PendingImage
                    {
                        width = renderTexture.width,
                        height = renderTexture.height,
                        data = encodedImageData.ToArray()
                    };

                    if (!m_PendingEncodedImages.TryAdd(frameCount, pendingImage))
                    {
                        throw new InvalidOperationException("Could not add encoded png to pending encoded images");
                    }

                    ReportFrameIfReady(frameCount);
                });
            }
        }

        /// <inheritdoc/>
        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
        {
            m_PendingFutures[Time.frameCount] =
                perceptionCamera.SensorHandle.ReportAnnotationAsync(m_Definition);
        }

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("InstanceSegmentationLabeler's idLabelConfig field must be assigned");

            m_Definition = new InstanceSegmentationDefinition(annotationId, idLabelConfig.GetAnnotationSpecification());
            DatasetCapture.RegisterAnnotationDefinition(m_Definition);

            perceptionCamera.InstanceSegmentationImageReadback += OnImageCaptured;
            perceptionCamera.RenderedObjectInfosCalculated += OnRenderedObjectInfosCalculated;

            m_PendingFutures = new Dictionary<int, AsyncFuture<DataModel.Annotation>>();
            m_PendingEntries = new ConcurrentDictionary<int, List<InstanceSegmentationEntry>>();
            m_PendingEncodedImages = new ConcurrentDictionary<int, PendingImage>();

            visualizationEnabled = supportsVisualization;
        }
    }
}
