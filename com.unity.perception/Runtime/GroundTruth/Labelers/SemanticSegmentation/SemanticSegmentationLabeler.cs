using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Rendering;

#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Labeler which generates a semantic segmentation image each frame. Each object is rendered to the semantic segmentation
    /// image using the color associated with it based on the given <see cref="SemanticSegmentationLabelConfig"/>.
    /// Semantic segmentation images are saved to the dataset in PNG format.
    /// Only one SemanticSegmentationLabeler can render at once across all cameras.
    /// </summary>
    [Serializable]
    public sealed class SemanticSegmentationLabeler : CameraLabeler, IOverlayPanelProvider
    {
        SemanticSegmentationDefinition m_AnnotationDefinition;

        public string annotationId = "semantic segmentation";

        /// <inheritdoc />
        public override string labelerId => annotationId;

        /// <summary>
        /// The SemanticSegmentationLabelConfig which maps labels to pixel values.
        /// </summary>
        public SemanticSegmentationLabelConfig labelConfig;

        /// <summary>
        /// The encoding format used when writing the captured segmentation images.
        /// </summary>
        const LosslessImageEncodingFormat k_ImageEncodingFormat = LosslessImageEncodingFormat.Png;

        /// <summary>
        /// Event information for <see cref="SemanticSegmentationLabeler.imageReadback"/>
        /// </summary>
        public struct ImageReadbackEventArgs
        {
            /// <summary>
            /// The <see cref="Time.frameCount"/> on which the image was rendered. This may be multiple frames in the past.
            /// </summary>
            public int frameCount;

            /// <summary>
            /// Color pixel data.
            /// </summary>
            public NativeArray<Color32> data;

            /// <summary>
            /// The source image texture.
            /// </summary>
            public RenderTexture sourceTexture;
        }

        /// <summary>
        /// Event which is called each frame a semantic segmentation image is read back from the GPU.
        /// </summary>
        public event Action<ImageReadbackEventArgs> imageReadback;

        /// <summary>
        /// The RenderTexture on which semantic segmentation images are drawn. Will be resized on startup to match
        /// the camera resolution.
        /// </summary>
        public RenderTexture targetTexture => m_TargetTextureOverride;

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public Texture overlayImage=> targetTexture;

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public string label => "SemanticSegmentation";

        [Tooltip("(Optional) The RenderTexture on which semantic segmentation images will be drawn. Will be reformatted on startup.")]
        [SerializeField]
        RenderTexture m_TargetTextureOverride;

#if HDRP_PRESENT
        SemanticSegmentationPass m_SemanticSegmentationPass;
        LensDistortionPass m_LensDistortionPass;
    #elif URP_PRESENT
        SemanticSegmentationUrpPass m_SemanticSegmentationPass;
        LensDistortionUrpPass m_LensDistortionPass;
    #endif

        Dictionary<int, AsyncFuture<Annotation>> m_AsyncAnnotations;

        /// <summary>
        /// Creates a new SemanticSegmentationLabeler.
        /// Be sure to assign <see cref="labelConfig"/> before adding to a <see cref="PerceptionCamera"/>.
        /// </summary>
        public SemanticSegmentationLabeler() { }

        /// <summary>
        /// Creates a new SemanticSegmentationLabeler with the given <see cref="SemanticSegmentationLabelConfig"/>.
        /// </summary>
        /// <param name="labelConfig">The label config associating labels with colors.</param>
        /// <param name="targetTextureOverride">Override the target texture of the labeler. Will be reformatted on startup.</param>
        public SemanticSegmentationLabeler(SemanticSegmentationLabelConfig labelConfig, RenderTexture targetTextureOverride = null)
        {
            this.labelConfig = labelConfig;
            m_TargetTextureOverride = targetTextureOverride;
        }

        /// <inheritdoc/>
        public override string description => SemanticSegmentationDefinition.labelerDescription;

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <inheritdoc/>
        protected override void Setup()
        {
            var myCamera = perceptionCamera.GetComponent<Camera>();
            var camWidth = myCamera.pixelWidth;
            var camHeight = myCamera.pixelHeight;

            if (labelConfig == null)
            {
                throw new InvalidOperationException(
                    "SemanticSegmentationLabeler's LabelConfig must be assigned");
            }

            m_AsyncAnnotations = new Dictionary<int, AsyncFuture<Annotation>>();

            if (targetTexture != null)
            {
                if (targetTexture.sRGB)
                {
                    Debug.LogError("targetTexture supplied to SemanticSegmentationLabeler must be in Linear mode. Disabling labeler.");
                    enabled = false;
                }
                var renderTextureDescriptor = new RenderTextureDescriptor(camWidth, camHeight, GraphicsFormat.R8G8B8A8_UNorm, 8);
                targetTexture.descriptor = renderTextureDescriptor;
            }
            else
                m_TargetTextureOverride = new RenderTexture(camWidth, camHeight, 8, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            targetTexture.Create();
            targetTexture.name = "Labeling";

#if HDRP_PRESENT
            var gameObject = perceptionCamera.gameObject;
            var customPassVolume = gameObject.GetComponent<CustomPassVolume>() ?? gameObject.AddComponent<CustomPassVolume>();
            customPassVolume.injectionPoint = CustomPassInjectionPoint.BeforeRendering;
            customPassVolume.isGlobal = true;
            m_SemanticSegmentationPass = new SemanticSegmentationPass(myCamera, targetTexture, labelConfig)
            {
                name = "Labeling Pass"
            };
            customPassVolume.customPasses.Add(m_SemanticSegmentationPass);

            m_LensDistortionPass = new LensDistortionPass(myCamera, targetTexture)
            {
                name = "Lens Distortion Pass"
            };
            customPassVolume.customPasses.Add(m_LensDistortionPass);
#elif URP_PRESENT
            // Semantic Segmentation
            m_SemanticSegmentationPass = new SemanticSegmentationUrpPass(myCamera, targetTexture, labelConfig);
            perceptionCamera.AddScriptableRenderPass(m_SemanticSegmentationPass);

            // Lens Distortion
            m_LensDistortionPass = new LensDistortionUrpPass(myCamera, targetTexture);
            perceptionCamera.AddScriptableRenderPass(m_LensDistortionPass);
#endif
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

            m_AnnotationDefinition = new SemanticSegmentationDefinition(annotationId, specs);
            DatasetCapture.RegisterAnnotationDefinition(m_AnnotationDefinition);

            visualizationEnabled = supportsVisualization;
        }

        void OnSemanticSegmentationImageRead(int frameCount, NativeArray<Color32> data)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var future))
                return;

            m_AsyncAnnotations.Remove(frameCount);

            imageReadback?.Invoke(new ImageReadbackEventArgs
            {
                data = data,
                frameCount = frameCount,
                sourceTexture = targetTexture
            });

            ImageEncoder.EncodeImage(data, targetTexture.width, targetTexture.height,
                targetTexture.graphicsFormat, k_ImageEncodingFormat, encodedImageData =>
            {
                var toReport = new SemanticSegmentationAnnotation(
                    m_AnnotationDefinition, perceptionCamera.SensorHandle.Id, ImageEncoder.ConvertFormat(k_ImageEncodingFormat),
                    new Vector2(targetTexture.width, targetTexture.height),
                    m_AnnotationDefinition.spec, encodedImageData.ToArray());

                future.Report(toReport);
            });
        }

        /// <inheritdoc/>
        protected override void OnEndRendering(ScriptableRenderContext scriptableRenderContext)
        {
            m_AsyncAnnotations[Time.frameCount] = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition);
            RenderTextureReader.Capture<Color32>(scriptableRenderContext, targetTexture,
                (frameCount, data, renderTexture) => OnSemanticSegmentationImageRead(frameCount, data));
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {

            if (m_TargetTextureOverride != null)
                m_TargetTextureOverride.Release();

            m_TargetTextureOverride = null;
        }
    }
}
