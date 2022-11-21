using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    ///  Produces instance segmentation for each frame.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public sealed class InstanceSegmentationLabeler : CameraLabeler, IOverlayPanelProvider
    {
        RenderTexture m_InstanceIndicesTexture;
        RenderTexture m_InstanceSegmentationColorTexture;
        InstanceSegmentationDefinition m_Definition;
        Dictionary<int, AsyncFuture<Annotation>> m_PendingFutures = new Dictionary<int, AsyncFuture<Annotation>>();
        Dictionary<int, List<InstanceSegmentationEntry>> m_PendingEntries = new Dictionary<int, List<InstanceSegmentationEntry>>();
        Dictionary<int, NativeArray<byte>> m_PendingEncodedImages = new Dictionary<int, NativeArray<byte>>();

        /// <summary>
        /// The encoding format used when writing the captured segmentation images.
        /// </summary>
        const LosslessImageEncodingFormat k_ImageEncodingFormat = LosslessImageEncodingFormat.Png;

        /// <summary>
        /// An event called each frame after the instance segmentation image is read back from the GPU.
        /// The first returned parameter is the Time.frameCount when the frame was captured, the second is the
        /// readback pixel data, and the final parameter is the source segmentation texture.
        /// </summary>
        public Action<int, NativeArray<Color32>, RenderTexture> imageReadback;

        /// <summary>
        /// The GUID to associate with annotations produced by this labeler.
        /// </summary>
        [Tooltip("The id to associate with instance segmentation annotations in the dataset.")]
        public string annotationId = "instance segmentation";

        /// <summary>
        /// The <see cref="idLabelConfig"/> which associates objects with labels.
        /// </summary>
        public IdLabelConfig idLabelConfig;

        ///<inheritdoc/>
        public override string description => InstanceSegmentationDefinition.labelDescription;

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <inheritdoc />
        public override string labelerId => annotationId;

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public string label => "InstanceSegmentation";

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public Texture overlayImage => m_InstanceSegmentationColorTexture;

        /// <summary>
        /// Creates a new InstanceSegmentationLabeler.
        /// Be sure to assign <see cref="idLabelConfig"/> before adding to a <see cref="PerceptionCamera"/>.
        /// </summary>
        public InstanceSegmentationLabeler() {}

        /// <summary>
        /// Creates a new InstanceSegmentationLabeler with the given <see cref="IdLabelConfig"/>.
        /// </summary>
        /// <param name="labelConfig">The label config for resolving the label for each object.</param>
        public InstanceSegmentationLabeler(IdLabelConfig labelConfig)
        {
            idLabelConfig = labelConfig;
        }

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("InstanceSegmentationLabeler's idLabelConfig field must be assigned");

            var sensor = perceptionCamera.cameraSensor;
            m_InstanceSegmentationColorTexture = new RenderTexture(
                sensor.pixelWidth, sensor.pixelHeight, 0, GraphicsFormat.R8G8B8A8_UNorm)
            {
                name = "Instance Segmentation Color Texture",
                filterMode = FilterMode.Point,
                enableRandomWrite = true
            };
            m_InstanceSegmentationColorTexture.Create();

            m_Definition = new InstanceSegmentationDefinition(annotationId, idLabelConfig.GetAnnotationSpecification());
            DatasetCapture.RegisterAnnotationDefinition(m_Definition);

            var channel = perceptionCamera.EnableChannel<InstanceIdChannel>();
            m_InstanceIndicesTexture = channel.outputTexture;
            perceptionCamera.RenderedObjectInfosCalculated += OnRenderedObjectInfosCalculated;

            visualizationEnabled = supportsVisualization;
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
            if (m_InstanceSegmentationColorTexture != null)
            {
                m_InstanceSegmentationColorTexture.Release();
                m_InstanceSegmentationColorTexture = null;
            }
        }

        /// <inheritdoc/>
        protected override void OnEndRendering(ScriptableRenderContext ctx)
        {
            m_PendingFutures[Time.frameCount] =
                perceptionCamera.SensorHandle.ReportAnnotationAsync(m_Definition);

            // Get a new CommandBuffer.
            var cmd = CommandBufferPool.Get("Color Instance Segmentation");

            // Create a compute buffer that maps instanceIndices to unique instance segmentation colors.
            var instanceIndices = LabelManager.singleton.instanceIds;
            var instanceSegmentationColors = LabelManager.singleton.instanceSegmentationColors;
            var colorBuffer = new ComputeBuffer(instanceIndices.Length, sizeof(uint));
            cmd.SetBufferData(colorBuffer, instanceSegmentationColors.AsArray(), 0, 0, colorBuffer.count);

            // Use a compute shader to map each pixel instance index to a unique color
            // to create the instance segmentation color texture.
            SegmentationUtilities.CreateSegmentationColorTexture(
                cmd, m_InstanceIndicesTexture, m_InstanceSegmentationColorTexture, colorBuffer);

            // Readback the m_InstanceSegmentationColorTexture.
            RenderTextureReader.Capture<Color32>(cmd, m_InstanceSegmentationColorTexture, (frame, data, texture) =>
            {
                imageReadback?.Invoke(frame, data, texture);
                ImageEncoder.EncodeImage(data, texture.width, texture.height,
                    texture.graphicsFormat, k_ImageEncodingFormat, encodedImageData =>
                    {
                        m_PendingEncodedImages[frame] = new NativeArray<byte>(encodedImageData, Allocator.Persistent);
                        ReportFrameIfReady(frame);
                    }
                );
                colorBuffer.Dispose();
            });

            ctx.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void OnRenderedObjectInfosCalculated(
            int frame,
            NativeArray<RenderedObjectInfo> renderedObjectInfos,
            SceneHierarchyInformation hierarchyInfo
        )
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
                    color = objectInfo.instanceColor
                });
            }

            m_PendingEntries[frame] = instances;

            ReportFrameIfReady(frame);
        }

        void ReportFrameIfReady(int frame)
        {
            if (!m_PendingFutures.ContainsKey(frame) ||
                !m_PendingEntries.ContainsKey(frame) ||
                !m_PendingEncodedImages.ContainsKey(frame))
                return;

            var future = m_PendingFutures[frame];
            var entries = m_PendingEntries[frame];
            var encodedImage = m_PendingEncodedImages[frame];

            m_PendingFutures.Remove(frame);
            m_PendingEntries.Remove(frame);
            m_PendingEncodedImages.Remove(frame);

            var toReport = new InstanceSegmentationAnnotation(
                m_Definition, perceptionCamera.SensorHandle.Id, entries,
                ImageEncoder.ConvertFormat(k_ImageEncodingFormat),
                new Vector2(
                    m_InstanceSegmentationColorTexture.width,
                    m_InstanceSegmentationColorTexture.height
                ),
                encodedImage.ToArray()
            );

            future.Report(toReport);
            encodedImage.Dispose();
        }
    }
}
