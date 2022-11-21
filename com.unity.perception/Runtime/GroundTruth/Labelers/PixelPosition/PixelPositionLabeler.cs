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
    /// The Pixel Position labeler outputs the camera-space distance from the camera to an object at a pixel.
    /// Imagine a vector v = (x,y,z) from the camera to the object at pixel.
    /// <list type="table">
    /// <listheader>
    ///     <term>Channel</term>
    ///     <description>Description</description>
    ///  </listheader>
    /// <item><term>Red</term><description>The "x" component of vector "v".</description></item>
    /// <item><term>Green</term><description>The "y" component of vector "v".</description></item>
    /// <item><term>Blue</term><description>The "z" component of vector "v". Represents eye-depth.</description></item>
    /// <item><term>Alpha</term><description>Always set to 1.</description></item>
    /// </list>
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public sealed class PixelPositionLabeler : CameraLabeler
    {
        RenderTexture m_PixelPositionTexture;
        PixelPositionDefinition m_AnnotationDefinition;
        Dictionary<int, AsyncFuture<Annotation>> m_AsyncAnnotations;

        /// <summary>
        /// The encoding format used when writing the captured segmentation images.
        /// </summary>
        const LosslessImageEncodingFormat k_ImageEncodingFormat = LosslessImageEncodingFormat.Exr;

        /// <summary>
        /// The string id used to identify this labeler in the dataset.
        /// </summary>
        public string annotationId = "PixelPosition";

        /// <inheritdoc />
        public override string labelerId => annotationId;

        /// <inheritdoc/>
        public override string description => PixelPositionDefinition.labelerDescription;

        /// <inheritdoc/>
        protected override bool supportsVisualization => false;

        /// <summary>
        /// Creates a new PixelPositionLabeler.
        /// </summary>
        public PixelPositionLabeler() {}

        /// <inheritdoc/>
        protected override void Setup()
        {
            var channel = perceptionCamera.EnableChannel<PixelPositionChannel>();
            channel.outputTextureReadback += OnPixelPositionImageRead;
            m_PixelPositionTexture = channel.outputTexture;

            m_AsyncAnnotations = new Dictionary<int, AsyncFuture<Annotation>>();

            m_AnnotationDefinition = new PixelPositionDefinition(annotationId);
            DatasetCapture.RegisterAnnotationDefinition(m_AnnotationDefinition);

            visualizationEnabled = supportsVisualization;
        }

        void OnPixelPositionImageRead(int frameCount, NativeArray<float4> data)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var future))
                return;

            m_AsyncAnnotations.Remove(frameCount);

            ImageEncoder.EncodeImage(data, m_PixelPositionTexture.width, m_PixelPositionTexture.height,
                m_PixelPositionTexture.graphicsFormat, k_ImageEncodingFormat, encodedImageData =>
                {
                    var toReport = new PixelPositionAnnotation(
                        m_AnnotationDefinition, perceptionCamera.SensorHandle.Id,
                        ImageEncoder.ConvertFormat(k_ImageEncodingFormat),
                        new Vector2(m_PixelPositionTexture.width, m_PixelPositionTexture.height),
                        encodedImageData.ToArray()
                    );
                    future.Report(toReport);
                });
        }

        /// <inheritdoc/>
        protected override void OnEndRendering(ScriptableRenderContext ctx)
        {
            m_AsyncAnnotations[Time.frameCount] = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition);
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
            m_PixelPositionTexture = null;
        }
    }
}
