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

        [NonSerialized]
        internal RenderTexture semanticSegmentationTexture;

        AnnotationDefinition m_SemanticSegmentationAnnotationDefinition;
        RenderTextureReader<Color32> m_SemanticSegmentationTextureReader;

#if HDRP_PRESENT
        SemanticSegmentationPass m_SemanticSegmentationPass;
#endif

        Dictionary<int, AsyncAnnotation> m_AsyncAnnotations;

        /// <summary>
        /// Creates a new SemanticSegmentationLabeler. Be sure to assign <see cref="labelConfig"/> before adding to a <see cref="PerceptionCamera"/>.
        /// </summary>
        public SemanticSegmentationLabeler() { }

        /// <summary>
        /// Creates a new SemanticSegmentationLabeler with the given <see cref="SemanticSegmentationLabelConfig"/>.
        /// </summary>
        /// <param name="labelConfig">The label config associating labels with colors.</param>
        public SemanticSegmentationLabeler(SemanticSegmentationLabelConfig labelConfig)
        {
            this.labelConfig = labelConfig;
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

        protected override void Setup()
        {
            var myCamera = perceptionCamera.GetComponent<Camera>();
            var width = myCamera.pixelWidth;
            var height = myCamera.pixelHeight;

            if (labelConfig == null)
            {
                throw new InvalidOperationException(
                    "SemanticSegmentationLabeler's LabelConfig must be assigned");
            }

            m_AsyncAnnotations = new Dictionary<int, AsyncAnnotation>();

            semanticSegmentationTexture = new RenderTexture(
                new RenderTextureDescriptor(width, height, GraphicsFormat.R8G8B8A8_UNorm, 8));
            semanticSegmentationTexture.name = "Labeling";

#if HDRP_PRESENT
            var gameObject = perceptionCamera.gameObject;
            var customPassVolume = gameObject.GetComponent<CustomPassVolume>() ?? gameObject.AddComponent<CustomPassVolume>();
            customPassVolume.injectionPoint = CustomPassInjectionPoint.BeforeRendering;
            customPassVolume.isGlobal = true;
            m_SemanticSegmentationPass = new SemanticSegmentationPass(myCamera, semanticSegmentationTexture, labelConfig)
            {
                name = "Labeling Pass"
            };
            customPassVolume.customPasses.Add(m_SemanticSegmentationPass);
#endif
#if URP_PRESENT
            perceptionCamera.AddScriptableRenderPass(new SemanticSegmentationUrpPass(myCamera, semanticSegmentationTexture, labelConfig));
#endif

            var specs = labelConfig.labelEntries.Select((l) => new SemanticSegmentationSpec()
            {
                label_name = l.label,
                pixel_value = l.color
            }).ToArray();

            m_SemanticSegmentationAnnotationDefinition = SimulationManager.RegisterAnnotationDefinition(
                "semantic segmentation",
                specs,
                "pixel-wise semantic segmentation label",
                "PNG",
                id: Guid.Parse(annotationId));

            m_SemanticSegmentationTextureReader = new RenderTextureReader<Color32>(semanticSegmentationTexture, myCamera,
                (frameCount, data, tex) => OnSemanticSegmentationImageRead(frameCount, data));

            SimulationManager.SimulationEnding += Cleanup;
        }

        void OnSemanticSegmentationImageRead(int frameCount, NativeArray<Color32> data)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var annotation))
                return;

            var datasetRelativePath = Path.Combine(k_SemanticSegmentationDirectory, k_SegmentationFilePrefix) + frameCount + ".png";
            var localPath = Path.Combine(Manager.Instance.GetDirectoryFor(k_SemanticSegmentationDirectory), k_SegmentationFilePrefix) + frameCount + ".png";

            annotation.ReportFile(datasetRelativePath);

            var asyncRequest = Manager.Instance.CreateRequest<AsyncRequest<AsyncSemanticSegmentationWrite>>();
            imageReadback?.Invoke(new ImageReadbackEventArgs
            {
                data = data,
                frameCount = frameCount,
                sourceTexture = semanticSegmentationTexture
            });
            asyncRequest.data = new AsyncSemanticSegmentationWrite
            {
                data = new NativeArray<Color32>(data, Allocator.TempJob),
                width = semanticSegmentationTexture.width,
                height = semanticSegmentationTexture.height,
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

        protected override void OnBeginRendering()
        {
            m_AsyncAnnotations[Time.frameCount] = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_SemanticSegmentationAnnotationDefinition);
        }

        protected override void Cleanup()
        {
            m_SemanticSegmentationTextureReader?.WaitForAllImages();
            m_SemanticSegmentationTextureReader?.Dispose();
            m_SemanticSegmentationTextureReader = null;

            if (semanticSegmentationTexture != null)
                semanticSegmentationTexture.Release();

            semanticSegmentationTexture = null;
        }
    }
}
