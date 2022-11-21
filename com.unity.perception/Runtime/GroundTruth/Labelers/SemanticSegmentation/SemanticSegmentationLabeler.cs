using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
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
    /// Labeler which generates a semantic segmentation image each frame. Each object is rendered to the semantic segmentation
    /// image using the color associated with it based on the given <see cref="SemanticSegmentationLabelConfig"/>.
    /// Semantic segmentation images are saved to the dataset in PNG format.
    /// Only one SemanticSegmentationLabeler can render at once across all cameras.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public sealed class SemanticSegmentationLabeler : CameraLabeler, IOverlayPanelProvider
    {
        RenderTexture m_InstanceIndicesTexture;
        RenderTexture m_SemanticSegmentationColorTexture;
        SemanticSegmentationDefinition m_AnnotationDefinition;
        Dictionary<int, AsyncFuture<Annotation>> m_PendingFutures = new Dictionary<int, AsyncFuture<Annotation>>();
        Dictionary<int, List<SemanticSegmentationDefinitionEntry>> m_PendingEntries =
            new Dictionary<int, List<SemanticSegmentationDefinitionEntry>>();
        Dictionary<int, NativeArray<byte>> m_PendingEncodedImages = new Dictionary<int, NativeArray<byte>>();
        Dictionary<int, NativeArray<Color32>> m_LabeledObjectColors = new Dictionary<int, NativeArray<Color32>>();

        /// <summary>
        /// The encoding format used when writing the captured segmentation images.
        /// </summary>
        const LosslessImageEncodingFormat k_ImageEncodingFormat = LosslessImageEncodingFormat.Png;

        /// <summary>
        /// Event which is called each frame a semantic segmentation image is read back from the GPU.
        /// The first returned parameter is the Time.frameCount when the frame was captured, the second is the
        /// readback pixel data, and the final parameter is the source segmentation texture.
        /// </summary>
        public event Action<int, NativeArray<Color32>, RenderTexture> imageReadback;

        /// <summary>
        /// The string id used to identify this labeler in the dataset.
        /// </summary>
        public string annotationId = "semantic segmentation";

        /// <summary>
        /// The SemanticSegmentationLabelConfig which maps labels to pixel values.
        /// </summary>
        public SemanticSegmentationLabelConfig labelConfig;

        /// <inheritdoc />
        public override string labelerId => annotationId;

        /// <inheritdoc/>
        public override string description => SemanticSegmentationDefinition.labelerDescription;

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public string label => $"SemanticSegmentation {annotationId}";

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public Texture overlayImage => m_SemanticSegmentationColorTexture;

        /// <summary>
        /// Creates a new SemanticSegmentationLabeler.
        /// Be sure to assign <see cref="labelConfig"/> before adding to a <see cref="PerceptionCamera"/>.
        /// </summary>
        public SemanticSegmentationLabeler() {}

        /// <summary>
        /// Creates a new SemanticSegmentationLabeler with the given <see cref="SemanticSegmentationLabelConfig"/>.
        /// </summary>
        /// <param name="labelConfig">The label config associating labels with colors.</param>
        public SemanticSegmentationLabeler(SemanticSegmentationLabelConfig labelConfig)
        {
            this.labelConfig = labelConfig;
        }

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (labelConfig == null)
            {
                throw new InvalidOperationException(
                    "SemanticSegmentationLabeler's LabelConfig must be assigned");
            }

            var channel = perceptionCamera.EnableChannel<InstanceIdChannel>();
            m_InstanceIndicesTexture = channel.outputTexture;
            perceptionCamera.RenderedObjectInfosCalculated += OnRenderedObjectInfosCalculated;

            var sensor = perceptionCamera.cameraSensor;
            m_SemanticSegmentationColorTexture = new RenderTexture(
                new RenderTextureDescriptor(sensor.pixelWidth, sensor.pixelHeight, GraphicsFormat.R8G8B8A8_UNorm, 0))
            {
                name = $"Semantic Segmentation {annotationId}",
                filterMode = FilterMode.Point,
                enableRandomWrite = true
            };
            m_SemanticSegmentationColorTexture.Create();

            var specs = labelConfig.labelEntries.Select(l => new SemanticSegmentationDefinitionEntry
            {
                labelName = l.label,
                pixelValue = l.color
            });

            if (labelConfig.skyColor != Color.black)
            {
                specs = specs.Append(new SemanticSegmentationDefinitionEntry
                {
                    labelName = "sky",
                    pixelValue = labelConfig.skyColor
                });
            }

            m_AnnotationDefinition = new SemanticSegmentationDefinition(annotationId, specs.ToList());
            DatasetCapture.RegisterAnnotationDefinition(m_AnnotationDefinition);

            visualizationEnabled = supportsVisualization;
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
            if (m_SemanticSegmentationColorTexture != null)
                m_SemanticSegmentationColorTexture.Release();
            m_SemanticSegmentationColorTexture = null;
        }

        /// <inheritdoc/>
        protected override void OnEndRendering(ScriptableRenderContext ctx)
        {
            var frame = Time.frameCount;
            m_PendingFutures[frame] =
                perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition);

            // Get a new CommandBuffer.
            var cmd = CommandBufferPool.Get("Semantic Segmentation");

            // Create a compute buffer that maps instanceIndices to unique instance segmentation colors.
            var instanceIndices = LabelManager.singleton.instanceIds;
            var colorBuffer = new ComputeBuffer(instanceIndices.Length, sizeof(uint));
            var labeledObjectColors = GetSegmentationColorForEachLabeledObject();
            m_LabeledObjectColors[frame] = labeledObjectColors;
            cmd.SetBufferData(colorBuffer, labeledObjectColors, 0, 0, colorBuffer.count);

            // Use a compute shader to map each pixel instance index to a unique color
            // to create the instance segmentation color texture.
            SegmentationUtilities.CreateSegmentationColorTexture(
                cmd, m_InstanceIndicesTexture, m_SemanticSegmentationColorTexture, colorBuffer);

            // Readback the m_InstanceSegmentationColorTexture.
            RenderTextureReader.Capture<Color32>(cmd, m_SemanticSegmentationColorTexture,
                (captureFrame, data, texture) =>
                {
                    imageReadback?.Invoke(captureFrame, data, texture);
                    ImageEncoder.EncodeImage(data, texture.width, texture.height,
                        texture.graphicsFormat, k_ImageEncodingFormat, encodedImageData =>
                        {
                            m_PendingEncodedImages[captureFrame] = new NativeArray<byte>(
                                encodedImageData, Allocator.Persistent);
                            ReportFrameIfReady(captureFrame);
                        }
                    );
                    colorBuffer.Dispose();
                });

            ctx.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        NativeArray<Color32> GetSegmentationColorForEachLabeledObject()
        {
            var labeledObjectColors = new NativeArray<Color32>(
                LabelManager.singleton.instanceIds.Length, Allocator.Persistent);
            labeledObjectColors[0] = new Color32(0, 0, 0, 255);

            var i = 1;
            foreach (var labeledObject in LabelManager.singleton.registeredLabels)
            {
                if (labelConfig.TryGetMatchingConfigurationEntry(labeledObject, out var labelEntry))
                    labeledObjectColors[i] = labelEntry.color;
                else
                    labeledObjectColors[i] = new Color32(0, 0, 0, 255);
                i++;
            }
            return labeledObjectColors;
        }

        void OnRenderedObjectInfosCalculated(
            int frame, NativeArray<RenderedObjectInfo> renderedObjectInfos,
            SceneHierarchyInformation hierarchyInfo
        )
        {
            var labeledObjectColors = m_LabeledObjectColors[frame];
            m_LabeledObjectColors.Remove(frame);

            // Create a set of all the colors present in the semantic segmentation image.
            var colorSet = new HashSet<Color32>();
            foreach (var objectInfos in renderedObjectInfos)
                colorSet.Add(labeledObjectColors[(int)objectInfos.instanceIndex]);
            labeledObjectColors.Dispose();

            // Report only the colors that are present within the current segmentation image.
            m_PendingEntries[frame] = m_AnnotationDefinition.spec.Where(
                entry => colorSet.Contains(entry.pixelValue)).ToList();

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

            var toReport = new SemanticSegmentationAnnotation(
                m_AnnotationDefinition, perceptionCamera.SensorHandle.Id,
                ImageEncoder.ConvertFormat(k_ImageEncodingFormat),
                new Vector2(m_SemanticSegmentationColorTexture.width, m_SemanticSegmentationColorTexture.height),
                entries,
                encodedImage.ToArray());

            future.Report(toReport);
            encodedImage.Dispose();
        }
    }
}
