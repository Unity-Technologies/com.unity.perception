using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Simulation;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.UI;

#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Labeler which generates a semantic segmentation image each frame. Each object is rendered to the semantic segmentation
    /// image using the color associated with it based on the given <see cref="SemanticSegmentationLabelConfig"/>.
    /// Semantic segmentation images are saved to the dataset in PNG format.
    ///
    /// Only one SemanticSegmentationLabeler can render at once across all cameras.
    /// </summary>
    [Serializable]
    public sealed class SemanticSegmentationLabeler : CameraLabeler
    {
        ///<inheritdoc/>
        public override string description
        {
            get => "Generates a semantic segmentation image for each captured frame. Each object is rendered to the semantic segmentation image using the color associated with it based on this labeler's associated semantic segmentation label configuration. " +
                   "Semantic segmentation images are saved to the dataset in PNG format. " +
                   "Please note that only one " + this.GetType().Name + " can render at once across all cameras.";
            protected set {}
        }

        const string k_SemanticSegmentationDirectory = "SemanticSegmentation";
        const string k_SegmentationFilePrefix = "segmentation_";

        /// <summary>
        /// The id to associate with semantic segmentation annotations in the dataset.
        /// </summary>
        [Tooltip("The id to associate with semantic segmentation annotations in the dataset.")]
        public string annotationId = "12f94d8d-5425-4deb-9b21-5e53ad957d66";
        /// <summary>
        /// The SemanticSegmentationLabelConfig which maps labels to pixel values.
        /// </summary>
        public SemanticSegmentationLabelConfig labelConfig;

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

        [Tooltip("(Optional) The RenderTexture on which semantic segmentation images will be drawn. Will be reformatted on startup.")]
        [SerializeField]
        RenderTexture m_TargetTextureOverride;

        AnnotationDefinition m_SemanticSegmentationAnnotationDefinition;
        RenderTextureReader<Color32> m_SemanticSegmentationTextureReader;

        internal bool m_fLensDistortionEnabled = false;

    #if HDRP_PRESENT
        SemanticSegmentationPass m_SemanticSegmentationPass;
        LensDistortionPass m_LensDistortionPass;
    #elif URP_PRESENT
        SemanticSegmentationUrpPass m_SemanticSegmentationPass;
        LensDistortionUrpPass m_LensDistortionPass;
    #endif

        Dictionary<int, AsyncAnnotation> m_AsyncAnnotations;

        private float segmentTransparency = 0.8f;
        private float backgroundTransparency = 0.0f;

        /// <summary>
        /// Creates a new SemanticSegmentationLabeler. Be sure to assign <see cref="labelConfig"/> before adding to a <see cref="PerceptionCamera"/>.
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
            this.m_TargetTextureOverride = targetTextureOverride;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        struct SemanticSegmentationSpec
        {
            [UsedImplicitly]
            public string label_name;
            [UsedImplicitly]
            public Color pixel_value;
        }

        struct AsyncSemanticSegmentationWrite
        {
            public NativeArray<Color32> data;
            public int width;
            public int height;
            public string path;
        }

        int camWidth = 0;
        int camHeight = 0;

        private GameObject segCanvas;
        private GameObject segVisual = null;
        private RawImage segImage = null;

        GUIStyle labelStyle = null;
        GUIStyle sliderStyle = null;

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <inheritdoc/>
        protected override void Setup()
        {
            var myCamera = perceptionCamera.GetComponent<Camera>();
            camWidth = myCamera.pixelWidth;
            camHeight = myCamera.pixelHeight;

            if (labelConfig == null)
            {
                throw new InvalidOperationException(
                    "SemanticSegmentationLabeler's LabelConfig must be assigned");
            }

            m_AsyncAnnotations = new Dictionary<int, AsyncAnnotation>();

            if (targetTexture != null)
            {
                if (targetTexture.sRGB)
                {
                    Debug.LogError("targetTexture supplied to SemanticSegmentationLabeler must be in Linear mode. Disabling labeler.");
                    this.enabled = false;
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

            m_fLensDistortionEnabled = true;
#elif URP_PRESENT
            // Semantic Segmentation
            m_SemanticSegmentationPass = new SemanticSegmentationUrpPass(myCamera, targetTexture, labelConfig);
            perceptionCamera.AddScriptableRenderPass(m_SemanticSegmentationPass);

            // Lens Distortion

            m_LensDistortionPass = new LensDistortionUrpPass(myCamera, targetTexture);
            perceptionCamera.AddScriptableRenderPass(m_LensDistortionPass);

            m_fLensDistortionEnabled = true;
#endif

            var specs = labelConfig.labelEntries.Select((l) => new SemanticSegmentationSpec()
            {
                label_name = l.label,
                pixel_value = l.color
            }).ToArray();

            m_SemanticSegmentationAnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition(
                "semantic segmentation",
                specs,
                "pixel-wise semantic segmentation label",
                "PNG",
                id: Guid.Parse(annotationId));

            m_SemanticSegmentationTextureReader = new RenderTextureReader<Color32>(targetTexture, myCamera,
                (frameCount, data, tex) => OnSemanticSegmentationImageRead(frameCount, data));

            visualizationEnabled = supportsVisualization;
        }

        private void SetupVisualizationElements()
        {
            segmentTransparency = 0.8f;
            backgroundTransparency = 0.0f;

            segVisual = GameObject.Instantiate(Resources.Load<GameObject>("SegmentTexture"));

            segImage = segVisual.GetComponent<RawImage>();
            segImage.material.SetFloat("_SegmentTransparency", segmentTransparency);
            segImage.material.SetFloat("_BackTransparency", backgroundTransparency);
            segImage.texture = targetTexture;

            var rt = segVisual.transform as RectTransform;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, camWidth);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, camHeight);

            if (segCanvas == null)
            {
                segCanvas = new GameObject(perceptionCamera.gameObject.name + "_segmentation_canvas");
                segCanvas.AddComponent<RectTransform>();
                var canvas = segCanvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                segCanvas.AddComponent<CanvasScaler>();

                segVisual.transform.SetParent(segCanvas.transform, false);
            }

            labelStyle = new GUIStyle(GUI.skin.label) {padding = {left = 10}};
            sliderStyle = new GUIStyle(GUI.skin.horizontalSlider) {margin = {left = 12}};
        }

        void OnSemanticSegmentationImageRead(int frameCount, NativeArray<Color32> data)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var annotation))
                return;

            var datasetRelativePath = $"{k_SemanticSegmentationDirectory}/{k_SegmentationFilePrefix}{frameCount}.png";
            var localPath = $"{Manager.Instance.GetDirectoryFor(k_SemanticSegmentationDirectory)}/{k_SegmentationFilePrefix}{frameCount}.png";

            annotation.ReportFile(datasetRelativePath);

            var asyncRequest = Manager.Instance.CreateRequest<AsyncRequest<AsyncSemanticSegmentationWrite>>();

            imageReadback?.Invoke(new ImageReadbackEventArgs
            {
                data = data,
                frameCount = frameCount,
                sourceTexture = targetTexture
            });
            asyncRequest.data = new AsyncSemanticSegmentationWrite
            {
                data = new NativeArray<Color32>(data, Allocator.TempJob),
                width = targetTexture.width,
                height = targetTexture.height,
                path = localPath
            };
            asyncRequest.Enqueue((r) =>
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

        /// <inheritdoc/>
        protected override void OnBeginRendering()
        {
            m_AsyncAnnotations[Time.frameCount] = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_SemanticSegmentationAnnotationDefinition);
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
            m_SemanticSegmentationTextureReader?.WaitForAllImages();
            m_SemanticSegmentationTextureReader?.Dispose();
            m_SemanticSegmentationTextureReader = null;

            Object.Destroy(segCanvas);
            segCanvas = null;

            if (m_TargetTextureOverride != null)
                m_TargetTextureOverride.Release();

            m_TargetTextureOverride = null;
        }

        /// <inheritdoc/>
        override protected void OnVisualizerEnabledChanged(bool enabled)
        {
            if (segVisual != null)
                segVisual.SetActive(enabled);
        }



        /// <inheritdoc/>
        protected override void OnVisualizeAdditionalUI()
        {
            if (segImage == null)
            {
                SetupVisualizationElements();
            }

            var rt = segVisual.transform as RectTransform;
            if (rt != null && camHeight != Screen.height)
            {
                camHeight = Screen.height;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, camHeight);
            }

            if (rt != null && camWidth != Screen.width)
            {
                camWidth = Screen.width;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width);
            }

            GUILayout.Space(4);
            GUILayout.Label("Object Alpha:", labelStyle);
            segmentTransparency = GUILayout.HorizontalSlider(segmentTransparency, 0.0f, 1.0f, sliderStyle, GUI.skin.horizontalSliderThumb);
            GUILayout.Space(4);
            GUILayout.Label("Background Alpha:", labelStyle);
            backgroundTransparency = GUILayout.HorizontalSlider(backgroundTransparency, 0.0f, 1.0f, sliderStyle, GUI.skin.horizontalSliderThumb);
            GUI.skin.label.padding.left = 0;

            if (!GUI.changed) return;
            segImage.material.SetFloat("_SegmentTransparency", segmentTransparency);
            segImage.material.SetFloat("_BackTransparency", backgroundTransparency);

        }
    }
}
