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
    /// Labeler which generates Produces an image capturing the vertex normals of objects within the frame.
    /// Normal images are saved to the dataset in EXR format.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public sealed class NormalLabeler : CameraLabeler, IOverlayPanelProvider
    {
        NormalDefinition m_AnnotationDefinition;
        RenderTexture m_NormalsTexture;
        Dictionary<int, AsyncFuture<Annotation>> m_AsyncAnnotations;

        /// <summary>
        /// The encoding format used when writing the captured segmentation images.
        /// </summary>
        const LosslessImageEncodingFormat k_ImageEncodingFormat = LosslessImageEncodingFormat.Exr;

        /// <summary>
        /// The string id used to identify this labeler in the dataset.
        /// </summary>
        public string annotationId = "Normal";

        /// <inheritdoc />
        public override string labelerId => annotationId;

        /// <inheritdoc/>
        public override string description => NormalDefinition.labelerDescription;

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public string label => "Normal";

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public Texture overlayImage => m_NormalsTexture;

        /// <inheritdoc/>
        protected override void Setup()
        {
            m_AsyncAnnotations = new Dictionary<int, AsyncFuture<Annotation>>();

            var channel = perceptionCamera.EnableChannel<VertexNormalsChannel>();
            channel.outputTextureReadback += OnNormalsTextureReadback;
            m_NormalsTexture = channel.outputTexture;

            m_AnnotationDefinition = new NormalDefinition(annotationId);
            DatasetCapture.RegisterAnnotationDefinition(m_AnnotationDefinition);
            visualizationEnabled = supportsVisualization;
        }

        void OnNormalsTextureReadback(int frameCount, NativeArray<float4> data)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var future))
                return;

            m_AsyncAnnotations.Remove(frameCount);

            ImageEncoder.EncodeImage(data, m_NormalsTexture.width, m_NormalsTexture.height,
                m_NormalsTexture.graphicsFormat, k_ImageEncodingFormat, encodedImageData =>
                {
                    var toReport = new NormalAnnotation(
                        m_AnnotationDefinition, perceptionCamera.SensorHandle.Id, ImageEncoder.ConvertFormat(k_ImageEncodingFormat),
                        new Vector2(m_NormalsTexture.width, m_NormalsTexture.height), encodedImageData.ToArray());

                    future.Report(toReport);
                }
            );
        }

        /// <inheritdoc/>
        protected override void OnEndRendering(ScriptableRenderContext ctx)
        {
            m_AsyncAnnotations[Time.frameCount] = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition);
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
            m_NormalsTexture = null;
        }
    }
}
