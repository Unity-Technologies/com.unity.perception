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
        const string k_SemanticSegmentationDirectory = "SemanticSegmentation";
        const string k_SegmentationFilePrefix = "segmentation_";

        /// <summary>
        /// The id to associate with semantic segmentation annotations in the dataset.
        /// </summary>
        [Tooltip("The id to associate with semantic segmentation annotations in the dataset.")]
        public string annotationId = "12F94D8D-5425-4DEB-9B21-5E53AD957D66";
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

#if HDRP_PRESENT
        SemanticSegmentationPass m_SemanticSegmentationPass;
#endif

        Dictionary<int, AsyncAnnotation> m_AsyncAnnotations;

        private float defaultSegmentTransparency = 0.8f;
        private float defaultBackgroundTransparency = 0.0f;

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

        private GameObject segVisual = null;
        private Image segImage = null;

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

            var renderTextureDescriptor = new RenderTextureDescriptor(camWidth, camHeight, GraphicsFormat.R8G8B8A8_UNorm, 8);
            if (targetTexture != null)
                targetTexture.descriptor = renderTextureDescriptor;
            else
                m_TargetTextureOverride = new RenderTexture(renderTextureDescriptor);

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
#endif
#if URP_PRESENT
            perceptionCamera.AddScriptableRenderPass(new SemanticSegmentationUrpPass(myCamera, targetTexture, labelConfig));
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

        void OnSemanticSegmentationImageRead(int frameCount, NativeArray<Color32> data)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var annotation))
                return;

            var datasetRelativePath = Path.Combine(k_SemanticSegmentationDirectory, k_SegmentationFilePrefix) + frameCount + ".png";
            var localPath = Path.Combine(Manager.Instance.GetDirectoryFor(k_SemanticSegmentationDirectory), k_SegmentationFilePrefix) + frameCount + ".png";

            annotation.ReportFile(datasetRelativePath);

            var asyncRequest = Manager.Instance.CreateRequest<AsyncRequest<AsyncSemanticSegmentationWrite>>();
            
            if (visualizationEnabled)
                VisualizeSegmentationTexture(data, targetTexture);

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
            asyncRequest.Start((r) =>
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

            if (m_TargetTextureOverride != null)
                m_TargetTextureOverride.Release();

            m_TargetTextureOverride = null;
        }

        /// <inheritdoc/>
        protected override void PopulateVisualizationPanel(ControlPanel panel)
        {
            panel.AddToggleControl("Segmentation Information", enabled => { visualizationEnabled = enabled; });

            defaultSegmentTransparency = 0.8f;
            defaultBackgroundTransparency = 0.0f;

            panel.AddSliderControl("Object Alpha", defaultSegmentTransparency, val => {
                if (segImage != null) segImage.material.SetFloat("_SegmentTransparency", val);
            });

            panel.AddSliderControl("Background Alpha", defaultBackgroundTransparency, val => {
                if (segImage != null) segImage.material.SetFloat("_BackTransparency", val);
            });

            segVisual = GameObject.Instantiate(Resources.Load<GameObject>("SegmentTexture"));

            segImage = segVisual.GetComponent<Image>();
            segImage.material.SetFloat("_SegmentTransparency", defaultSegmentTransparency);
            segImage.material.SetFloat("_BackTransparency", defaultBackgroundTransparency);

            RectTransform rt = segVisual.transform as RectTransform;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, camWidth);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, camHeight);

            canvas.AddComponent(segVisual, setAsLowestElement: true);
        }

        void VisualizeSegmentationTexture(NativeArray<Color32> data, RenderTexture texture)
        {
            var cpuTexture = new Texture2D(texture.width, texture.height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
            cpuTexture.LoadRawTextureData(data);
            cpuTexture.Apply();
            
            segImage.material.SetTexture("_BaseMap", cpuTexture);
        }

        /// <inheritdoc/>
        override protected void OnVisualizerActiveStateChanged(bool enabled)
        {
            if (segVisual != null) 
                segVisual.SetActive(enabled);
        }
    }
}
