using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// A labeler that captures depth images.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public sealed class DepthLabeler : CameraLabeler, IOverlayPanelProvider
    {
        RenderTexture m_DepthTexture;
        DepthDefinition m_AnnotationDefinition;
        Dictionary<int, AsyncFuture<Annotation>> m_AsyncAnnotations;

        /// <summary>
        /// The string id used to identify this labeler in the dataset.
        /// </summary>
        public string annotationId = "Depth";

        /// <inheritdoc />
        public override string labelerId => annotationId;

        /// <summary>
        /// The encoding format used when writing the captured segmentation images.
        /// </summary>
        const LosslessImageEncodingFormat k_ImageEncodingFormat = LosslessImageEncodingFormat.Exr;

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public Texture overlayImage => m_DepthTexture;

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public string label => "Depth";

        /// <summary>
        /// The capture strategy to use when capturing depth values.
        /// </summary>
        [Tooltip("Capturing depth values returns the distance between the surface of the object and the forward " +
            "plane of the camera. Capturing range values returns the line of sight distance between the surface of " +
            "the object and the position of the camera.")]
        public DepthMeasurementStrategy measurementStrategy = DepthMeasurementStrategy.Depth;

        /// <inheritdoc/>
        public override string description => DepthDefinition.labelerDescription;

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <inheritdoc/>
        protected override void Setup()
        {
            m_AsyncAnnotations = new Dictionary<int, AsyncFuture<Annotation>>();

            if (measurementStrategy == DepthMeasurementStrategy.Depth)
            {
                var channel = perceptionCamera.EnableChannel<DepthChannel>();
                channel.outputTextureReadback += OnDepthTextureReadback;
                m_DepthTexture = channel.outputTexture;
            }
            else
            {
                var channel = perceptionCamera.EnableChannel<RangeChannel>();
                channel.outputTextureReadback += OnDepthTextureReadback;
                m_DepthTexture = channel.outputTexture;
            }

            m_AnnotationDefinition = new DepthDefinition(annotationId);
            DatasetCapture.RegisterAnnotationDefinition(m_AnnotationDefinition);
            visualizationEnabled = supportsVisualization;
        }

        void OnDepthTextureReadback(int frameCount, NativeArray<float4> data)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var future))
                return;

            m_AsyncAnnotations.Remove(frameCount);

            ImageEncoder.EncodeImage(data, m_DepthTexture.width, m_DepthTexture.height,
                m_DepthTexture.graphicsFormat, k_ImageEncodingFormat, encodedImageData =>
                {
                    var toReport = new DepthAnnotation(
                        m_AnnotationDefinition,
                        perceptionCamera.SensorHandle.Id,
                        measurementStrategy,
                        ImageEncoder.ConvertFormat(k_ImageEncodingFormat),
                        new Vector2(m_DepthTexture.width, m_DepthTexture.height),
                        encodedImageData.ToArray());

                    future.Report(toReport);
                }
            );
        }

        /// <inheritdoc/>
        protected override void OnEndRendering(ScriptableRenderContext ctx)
        {
            m_AsyncAnnotations[Time.frameCount] =
                perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition);
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
            m_DepthTexture = null;
        }
    }
}
