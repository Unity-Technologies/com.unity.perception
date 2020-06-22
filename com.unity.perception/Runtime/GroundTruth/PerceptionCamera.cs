﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Profiling;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Captures ground truth from the associated Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PerceptionCamera : MonoBehaviour
    {
        const string k_SemanticSegmentationDirectory = "SemanticSegmentation";
        //TODO: Remove the Guid path when we have proper dataset merging in USim/Thea
        internal static string RgbDirectory { get; } = $"RGB{Guid.NewGuid()}";
        static string s_RgbFilePrefix = "rgb_";
        const string k_SegmentationFilePrefix = "segmentation_";

        /// <summary>
        /// A human-readable description of the camera.
        /// </summary>
        public string description;
        /// <summary>
        /// The period in seconds that the Camera should render
        /// </summary>
        public float period = .0166f;
        /// <summary>
        /// The start time in seconds of the first frame in the simulation.
        /// </summary>
        public float startTime;
        /// <summary>
        /// Whether camera output should be captured to disk
        /// </summary>
        public bool captureRgbImages = true;
        /// <summary>
        /// Whether semantic segmentation images should be generated
        /// </summary>
        public bool produceSegmentationImages = true;
        /// <summary>
        /// Whether object counts should be computed
        /// </summary>
        public bool produceObjectCountAnnotations = true;
        /// <summary>
        /// The ID to use for object count annotations in the resulting dataset
        /// </summary>
        [FormerlySerializedAs("m_ObjectCountID")]
        public string objectCountId = "51DA3C27-369D-4929-AEA6-D01614635CE2";
        /// <summary>
        /// Whether object bounding boxes should be computed
        /// </summary>
        public bool produceBoundingBoxAnnotations = true;
        /// <summary>
        /// The ID to use for bounding box annotations in the resulting dataset
        /// </summary>
        [FormerlySerializedAs("m_BoundingBoxID")]
        public string boundingBoxId = "F9F22E05-443F-4602-A422-EBE4EA9B55CB";
        /// <summary>
        /// Whether rendered object info metrics should be generated. This metric currently contains label id, instance id, and visible pixels.
        /// </summary>
        [FormerlySerializedAs("produceVisiblePixelsMetric")]
        public bool produceRenderedObjectInfoMetric = true;
        /// <summary>
        /// The ID to use for visible pixels metrics in the resulting dataset
        /// </summary>
        [FormerlySerializedAs("visiblePixelsId")]
        [FormerlySerializedAs("m_VisiblePixelsID")]
        public string renderedObjectInfoId = "5BA92024-B3B7-41A7-9D3F-C03A6A8DDD01";
        /// <summary>
        /// The corner of the image to use as the origin for bounding boxs.
        /// </summary>
        public BoundingBoxOrigin boundingBoxOrigin = BoundingBoxOrigin.TopLeft;
        /// <summary>
        /// The LabelingConfiguration to use for segmentation and object count.
        /// </summary>
        public LabelingConfiguration LabelingConfiguration;

        /// <summary>
        /// Invoked when RenderedObjectInfos are calculated. The first parameter is the Time.frameCount at which the objects were rendered. This may be called many frames after the frame in which the objects were rendered.
        /// </summary>
        public event Action<int, NativeArray<RenderedObjectInfo>> renderedObjectInfosCalculated;

        internal event Action<int, NativeArray<uint>> segmentationImageReceived;

        internal event Action<NativeSlice<uint>, IReadOnlyList<LabelEntry>, int> classCountsReceived;

        [NonSerialized]
        internal RenderTexture labelingTexture;
        [NonSerialized]
        internal RenderTexture segmentationTexture;

        RenderTextureReader<short> m_ClassLabelingTextureReader;
        RenderTextureReader<uint> m_SegmentationReader;
        RenderedObjectInfoGenerator m_RenderedObjectInfoGenerator;
        Dictionary<string, object> m_PersistentSensorData = new Dictionary<string, object>();

#if URP_PRESENT
        [NonSerialized]
        internal InstanceSegmentationUrpPass instanceSegmentationUrpPass;
        [NonSerialized]
        internal SemanticSegmentationUrpPass semanticSegmentationUrpPass;
#endif

        bool m_CapturedLastFrame;

        Ego m_EgoMarker;

        /// <summary>
        /// The <see cref="SensorHandle"/> associated with this camera. Use this to report additional annotations and metrics at runtime.
        /// </summary>
        public SensorHandle SensorHandle { get; private set; }

        struct AsyncSemanticSegmentationWrite
        {
            public short[] dataArray;
            public int width;
            public int height;
            public string path;
        }
        struct AsyncCaptureInfo
        {
            public int FrameCount;
            public AsyncAnnotation SegmentationAsyncAnnotation;
            public AsyncMetric ClassCountAsyncMetric;
            public AsyncMetric RenderedObjectInfoAsyncMetric;
            public AsyncAnnotation BoundingBoxAsyncMetric;
        }

        List<AsyncCaptureInfo> m_AsyncCaptureInfos = new List<AsyncCaptureInfo>();

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        struct ClassCountValue
        {
            public int label_id;
            public string label_name;
            public uint count;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        struct BoundingBoxValue
        {
            public int label_id;
            public string label_name;
            public int instance_id;
            public float x;
            public float y;
            public float width;
            public float height;
        }

        ClassCountValue[] m_ClassCountValues;
        BoundingBoxValue[] m_BoundingBoxValues;
        RenderedObjectInfoValue[] m_VisiblePixelsValues;

#if HDRP_PRESENT
        InstanceSegmentationPass m_SegmentationPass;
        SemanticSegmentationPass m_SemanticSegmentationPass;
#endif
        MetricDefinition m_ObjectCountMetricDefinition;
        AnnotationDefinition m_BoundingBoxAnnotationDefinition;
        AnnotationDefinition m_SegmentationAnnotationDefinition;
        MetricDefinition m_RenderedObjectInfoMetricDefinition;

        static ProfilerMarker s_WriteFrame = new ProfilerMarker("Write Frame (PerceptionCamera)");
        static ProfilerMarker s_FlipY = new ProfilerMarker("Flip Y (PerceptionCamera)");
        static ProfilerMarker s_EncodeAndSave = new ProfilerMarker("Encode and save (PerceptionCamera)");
        static ProfilerMarker s_ClassCountCallback = new ProfilerMarker("OnClassLabelsReceived");
        static ProfilerMarker s_RenderedObjectInfosCalculatedEvent = new ProfilerMarker("renderedObjectInfosCalculated event");
        static ProfilerMarker s_BoundingBoxCallback = new ProfilerMarker("OnBoundingBoxesReceived");
        static ProfilerMarker s_ProduceRenderedObjectInfoMetric = new ProfilerMarker("ProduceRenderedObjectInfoMetric");

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        struct SemanticSegmentationSpec
        {
            [UsedImplicitly]
            public int label_id;
            [UsedImplicitly]
            public string label_name;
            [UsedImplicitly]
            public int pixel_value;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        struct ObjectCountSpec
        {
            [UsedImplicitly]
            public int label_id;
            [UsedImplicitly]
            public string label_name;
        }

        /// <summary>
        /// Add a data object which will be added to the dataset with each capture. Overrides existing sensor data associated with the given key.
        /// </summary>
        /// <param name="key">The key to associate with the data.</param>
        /// <param name="data">An object containing the data. Will be serialized into json.</param>
        public void SetPersistentSensorData(string key, object data)
        {
            m_PersistentSensorData[key] = data;
        }

        /// <summary>
        /// Removes a persistent sensor data object.
        /// </summary>
        /// <param name="key">The key of the object to remove.</param>
        /// <returns>True if a data object was removed. False if it was not set.</returns>
        public bool RemovePersistentSensorData(string key)
        {
            return m_PersistentSensorData.Remove(key);
        }

        // Start is called before the first frame update
        void Awake()
        {
            //CaptureOptions.useAsyncReadbackIfSupported = false;

            m_EgoMarker = this.GetComponentInParent<Ego>();
            var ego = m_EgoMarker == null ? DatasetCapture.RegisterEgo("") : m_EgoMarker.EgoHandle;
            SensorHandle = DatasetCapture.RegisterSensor(ego, "camera", description, period, startTime);

            var myCamera = GetComponent<Camera>();
            var width = myCamera.pixelWidth;
            var height = myCamera.pixelHeight;

            if ((produceSegmentationImages || produceObjectCountAnnotations || produceBoundingBoxAnnotations) && LabelingConfiguration == null)
            {
                Debug.LogError("LabelingConfiguration must be set if producing ground truth data");
                produceSegmentationImages = false;
                produceObjectCountAnnotations = false;
                produceBoundingBoxAnnotations = false;
            }

            segmentationTexture = new RenderTexture(new RenderTextureDescriptor(width, height, GraphicsFormat.R8G8B8A8_UNorm, 8));
            segmentationTexture.name = "Segmentation";
            labelingTexture = new RenderTexture(new RenderTextureDescriptor(width, height, GraphicsFormat.R8G8B8A8_UNorm, 8));
            labelingTexture.name = "Labeling";

#if HDRP_PRESENT
            var customPassVolume = this.GetComponent<CustomPassVolume>() ?? gameObject.AddComponent<CustomPassVolume>();
            customPassVolume.injectionPoint = CustomPassInjectionPoint.BeforeRendering;
            customPassVolume.isGlobal = true;
            m_SegmentationPass = new InstanceSegmentationPass()
            {
                name = "Segmentation Pass",
                targetCamera = myCamera,
                targetTexture = segmentationTexture
            };
            m_SegmentationPass.EnsureInit();
            m_SemanticSegmentationPass = new SemanticSegmentationPass(myCamera, labelingTexture, LabelingConfiguration)
            {
                name = "Labeling Pass"
            };

            SetupPasses(customPassVolume);
#endif
#if URP_PRESENT
            instanceSegmentationUrpPass = new InstanceSegmentationUrpPass(myCamera, segmentationTexture);
            semanticSegmentationUrpPass = new SemanticSegmentationUrpPass(myCamera, labelingTexture, LabelingConfiguration);
#endif

            if (produceSegmentationImages)
            {
                var specs = LabelingConfiguration.LabelEntries.Select((l) => new SemanticSegmentationSpec()
                {
                    label_id = l.id,
                    label_name = l.label,
                    pixel_value = l.value
                }).ToArray();

                m_SegmentationAnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition("semantic segmentation", specs, "pixel-wise semantic segmentation label", "PNG");

                m_ClassLabelingTextureReader = new RenderTextureReader<short>(labelingTexture, myCamera,
                    (frameCount, data, tex) => OnSemanticSegmentationImageRead(frameCount, data));
            }

            if (produceObjectCountAnnotations || produceBoundingBoxAnnotations || produceRenderedObjectInfoMetric)
            {
                var labelingMetricSpec = LabelingConfiguration.LabelEntries.Select((l) => new ObjectCountSpec()
                {
                    label_id = l.id,
                    label_name = l.label,
                }).ToArray();

                if (produceObjectCountAnnotations)
                {
                    m_ObjectCountMetricDefinition = DatasetCapture.RegisterMetricDefinition("object count", labelingMetricSpec, "Counts of objects for each label in the sensor's view", id: new Guid(objectCountId));
                }

                if (produceBoundingBoxAnnotations)
                {
                    m_BoundingBoxAnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition("bounding box", labelingMetricSpec, "Bounding box for each labeled object visible to the sensor", id: new Guid(boundingBoxId));
                }

                if (produceRenderedObjectInfoMetric)
                    m_RenderedObjectInfoMetricDefinition = DatasetCapture.RegisterMetricDefinition("rendered object info", labelingMetricSpec, "Information about each labeled object visible to the sensor", id: new Guid(renderedObjectInfoId));

                m_RenderedObjectInfoGenerator = new RenderedObjectInfoGenerator(LabelingConfiguration);
                World.DefaultGameObjectInjectionWorld.GetExistingSystem<GroundTruthLabelSetupSystem>().Activate(m_RenderedObjectInfoGenerator);

                m_SegmentationReader = new RenderTextureReader<uint>(segmentationTexture, myCamera, (frameCount, data, tex) =>
                {
                    if (segmentationImageReceived != null)
                        segmentationImageReceived(frameCount, data);

                    m_RenderedObjectInfoGenerator.Compute(data, tex.width, boundingBoxOrigin, out var renderedObjectInfos, out var classCounts, Allocator.Temp);

                    using (s_RenderedObjectInfosCalculatedEvent.Auto())
                        renderedObjectInfosCalculated?.Invoke(frameCount, renderedObjectInfos);

                    if (produceObjectCountAnnotations)
                        OnObjectCountsReceived(classCounts, LabelingConfiguration.LabelEntries, frameCount);

                    if (produceBoundingBoxAnnotations)
                        ProduceBoundingBoxesAnnotation(renderedObjectInfos, LabelingConfiguration.LabelEntries, frameCount);

                    if (produceRenderedObjectInfoMetric)
                        ProduceRenderedObjectInfoMetric(renderedObjectInfos, frameCount);
                });
            }

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            DatasetCapture.SimulationEnding += OnSimulationEnding;
        }

        // ReSharper disable InconsistentNaming
        struct RenderedObjectInfoValue
        {
            [UsedImplicitly]
            public int label_id;
            [UsedImplicitly]
            public int instance_id;
            [UsedImplicitly]
            public int visible_pixels;
        }
        // ReSharper restore InconsistentNaming

        void ProduceRenderedObjectInfoMetric(NativeArray<RenderedObjectInfo> renderedObjectInfos, int frameCount)
        {
            using (s_ProduceRenderedObjectInfoMetric.Auto())
            {
                var findResult = FindAsyncCaptureInfo(frameCount);
                if (findResult.index == -1)
                    return;
                var asyncCaptureInfo = findResult.asyncCaptureInfo;
                var metric = asyncCaptureInfo.RenderedObjectInfoAsyncMetric;
                if (!metric.IsValid)
                    return;

                if (m_VisiblePixelsValues == null || m_VisiblePixelsValues.Length != renderedObjectInfos.Length)
                    m_VisiblePixelsValues = new RenderedObjectInfoValue[renderedObjectInfos.Length];

                for (var i = 0; i < renderedObjectInfos.Length; i++)
                {
                    var objectInfo = renderedObjectInfos[i];
                    if (!TryGetLabelEntryFromInstanceId(objectInfo.instanceId, out var labelEntry))
                        continue;

                    m_VisiblePixelsValues[i] = new RenderedObjectInfoValue
                    {
                        label_id = labelEntry.id,
                        instance_id = objectInfo.instanceId,
                        visible_pixels = objectInfo.pixelCount
                    };
                }

                metric.ReportValues(m_VisiblePixelsValues);
            }
        }

#if HDRP_PRESENT
        void SetupPasses(CustomPassVolume customPassVolume)
        {
            customPassVolume.customPasses.Remove(m_SegmentationPass);
            customPassVolume.customPasses.Remove(m_SemanticSegmentationPass);

            if (produceSegmentationImages || produceObjectCountAnnotations)
                customPassVolume.customPasses.Add(m_SegmentationPass);

            if (produceSegmentationImages)
                customPassVolume.customPasses.Add(m_SemanticSegmentationPass);
        }

#endif

        void ProduceBoundingBoxesAnnotation(NativeArray<RenderedObjectInfo> renderedObjectInfos, List<LabelEntry> labelingConfigurations, int frameCount)
        {
            using (s_BoundingBoxCallback.Auto())
            {
                var findResult = FindAsyncCaptureInfo(frameCount);
                if (findResult.index == -1)
                    return;
                var asyncCaptureInfo = findResult.asyncCaptureInfo;
                var boundingBoxAsyncAnnotation = asyncCaptureInfo.BoundingBoxAsyncMetric;
                if (!boundingBoxAsyncAnnotation.IsValid)
                    return;

                if (m_BoundingBoxValues == null || m_BoundingBoxValues.Length != renderedObjectInfos.Length)
                    m_BoundingBoxValues = new BoundingBoxValue[renderedObjectInfos.Length];

                for (var i = 0; i < renderedObjectInfos.Length; i++)
                {
                    var objectInfo = renderedObjectInfos[i];
                    if (!TryGetLabelEntryFromInstanceId(objectInfo.instanceId, out var labelEntry))
                        continue;

                    m_BoundingBoxValues[i] = new BoundingBoxValue
                    {
                        label_id = labelEntry.id,
                        label_name = labelEntry.label,
                        instance_id = objectInfo.instanceId,
                        x = objectInfo.boundingBox.x,
                        y = objectInfo.boundingBox.y,
                        width = objectInfo.boundingBox.width,
                        height = objectInfo.boundingBox.height,
                    };
                }

                boundingBoxAsyncAnnotation.ReportValues(m_BoundingBoxValues);
            }
        }

        /// <summary>
        /// Returns the class ID for the given instance ID resolved by <see cref="LabelingConfiguration"/>. Only valid when bounding boxes are being computed.
        /// </summary>
        /// <param name="instanceId">The instanceId of the object</param>
        /// <param name="labelEntry">When this method returns, contains the LabelEntry associated with the given instanceId if one exists. default otherwise.</param>
        /// <returns>True if a valid LabelEntry was found for the given instanceId.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="produceBoundingBoxAnnotations"/> was not true on Start.</exception>
        public bool TryGetLabelEntryFromInstanceId(int instanceId, out LabelEntry labelEntry)
        {
            if (m_RenderedObjectInfoGenerator == null)
                throw new InvalidOperationException($"{nameof(TryGetLabelEntryFromInstanceId)} can only be used when bounding box capture is enabled");
            return m_RenderedObjectInfoGenerator.TryGetLabelEntryFromInstanceId(instanceId, out labelEntry);
        }

        void OnObjectCountsReceived(NativeSlice<uint> counts, IReadOnlyList<LabelEntry> entries, int frameCount)
        {
            using (s_ClassCountCallback.Auto())
            {
                classCountsReceived?.Invoke(counts, entries, frameCount);

                var findResult = FindAsyncCaptureInfo(frameCount);
                if (findResult.index == -1)
                    return;

                var asyncCaptureInfo = findResult.asyncCaptureInfo;
                var classCountAsyncMetric = asyncCaptureInfo.ClassCountAsyncMetric;
                if (!classCountAsyncMetric.IsValid)
                    return;

                if (m_ClassCountValues == null || m_ClassCountValues.Length != entries.Count)
                    m_ClassCountValues = new ClassCountValue[entries.Count];

                for (var i = 0; i < entries.Count; i++)
                {
                    m_ClassCountValues[i] = new ClassCountValue()
                    {
                        label_id = entries[i].id,
                        label_name = entries[i].label,
                        count = counts[i]
                    };
                }

                classCountAsyncMetric.ReportValues(m_ClassCountValues);
            }
        }

        (int index, AsyncCaptureInfo asyncCaptureInfo) FindAsyncCaptureInfo(int frameCount)
        {
            for (var i = 0; i < m_AsyncCaptureInfos.Count; i++)
            {
                var captureInfo = m_AsyncCaptureInfos[i];
                if (captureInfo.FrameCount == frameCount)
                {
                    return (i, captureInfo);
                }
            }

            return (-1, default);
        }

        // Update is called once per frame
        void Update()
        {
            if (!SensorHandle.IsValid)
                return;

            var cam = GetComponent<Camera>();
            cam.enabled = SensorHandle.ShouldCaptureThisFrame;

            m_AsyncCaptureInfos.RemoveSwapBack(i =>
                !i.SegmentationAsyncAnnotation.IsPending &&
                !i.BoundingBoxAsyncMetric.IsPending &&
                !i.RenderedObjectInfoAsyncMetric.IsPending &&
                !i.ClassCountAsyncMetric.IsPending);
        }

        void ReportAsyncAnnotations()
        {
            if (produceSegmentationImages || produceObjectCountAnnotations || produceBoundingBoxAnnotations || produceRenderedObjectInfoMetric)
            {
                var captureInfo = new AsyncCaptureInfo()
                {
                    FrameCount = Time.frameCount
                };
                if (produceSegmentationImages)
                    captureInfo.SegmentationAsyncAnnotation = SensorHandle.ReportAnnotationAsync(m_SegmentationAnnotationDefinition);

                if (produceObjectCountAnnotations)
                    captureInfo.ClassCountAsyncMetric = SensorHandle.ReportMetricAsync(m_ObjectCountMetricDefinition);

                if (produceBoundingBoxAnnotations)
                    captureInfo.BoundingBoxAsyncMetric = SensorHandle.ReportAnnotationAsync(m_BoundingBoxAnnotationDefinition);

                if (produceRenderedObjectInfoMetric)
                    captureInfo.RenderedObjectInfoAsyncMetric = SensorHandle.ReportMetricAsync(m_RenderedObjectInfoMetricDefinition);

                m_AsyncCaptureInfos.Add(captureInfo);
            }
        }

        void CaptureRgbData(Camera cam)
        {
            Profiler.BeginSample("CaptureDataFromLastFrame");
            if (!captureRgbImages)
                return;

            var captureFilename = Path.Combine(Manager.Instance.GetDirectoryFor(RgbDirectory), $"{s_RgbFilePrefix}{Time.frameCount}.png");
            var dxRootPath = Path.Combine(RgbDirectory, $"{s_RgbFilePrefix}{Time.frameCount}.png");
            SensorHandle.ReportCapture(dxRootPath, SensorSpatialData.FromGameObjects(m_EgoMarker == null ? null : m_EgoMarker.gameObject, gameObject), m_PersistentSensorData.Select(kvp => (kvp.Key, kvp.Value)).ToArray());

            Func<AsyncRequest<CaptureCamera.CaptureState>, AsyncRequest.Result> colorFunctor = null;
            var width = cam.pixelWidth;
            var height = cam.pixelHeight;
            var flipY = ShouldFlipY(cam);

            colorFunctor = r =>
            {
                using (s_WriteFrame.Auto())
                {
                    var dataColorBuffer = (byte[])r.data.colorBuffer;
                    if (flipY)
                        FlipImageY(dataColorBuffer, height);

                    byte[] encodedData;
                    using (s_EncodeAndSave.Auto())
                    {
                        encodedData = ImageConversion.EncodeArrayToPNG(dataColorBuffer, GraphicsFormat.R8G8B8A8_UNorm, (uint)width, (uint)height);
                    }

                    return !FileProducer.Write(captureFilename, encodedData) ? AsyncRequest.Result.Error : AsyncRequest.Result.Completed;
                }
            };

            CaptureCamera.Capture(cam, colorFunctor);

            Profiler.EndSample();
        }

        // ReSharper disable once ParameterHidesMember
        bool ShouldFlipY(Camera camera)
        {
#if HDRP_PRESENT
            var hdAdditionalCameraData = GetComponent<HDAdditionalCameraData>();

            //Based on logic in HDRenderPipeline.PrepareFinalBlitParameters
            return camera.targetTexture != null || hdAdditionalCameraData.flipYMode == HDAdditionalCameraData.FlipYMode.ForceFlipY || camera.cameraType == CameraType.Game;
#elif URP_PRESENT
            return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal) &&
                (camera.targetTexture != null || camera.cameraType == CameraType.Game);
#else
            return false;
#endif
        }

        static unsafe void FlipImageY(byte[] dataColorBuffer, int height)
        {
            using (s_FlipY.Auto())
            {
                var stride = dataColorBuffer.Length / height;
                var buffer = new NativeArray<byte>(stride, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                fixed(byte* colorBufferPtr = &dataColorBuffer[0])
                {
                    var unsafePtr = (byte*)buffer.GetUnsafePtr();
                    for (var row = 0; row < height / 2; row++)
                    {
                        var nearRowStartPtr = colorBufferPtr + stride * row;
                        var oppositeRowStartPtr = colorBufferPtr + stride * (height - row - 1);
                        UnsafeUtility.MemCpy(unsafePtr, oppositeRowStartPtr, stride);
                        UnsafeUtility.MemCpy(oppositeRowStartPtr, nearRowStartPtr, stride);
                        UnsafeUtility.MemCpy(nearRowStartPtr, unsafePtr, stride);
                    }
                }
                buffer.Dispose();
            }
        }

        void OnSimulationEnding()
        {
            m_ClassLabelingTextureReader?.WaitForAllImages();
            m_ClassLabelingTextureReader?.Dispose();
            m_ClassLabelingTextureReader = null;

            m_SegmentationReader?.WaitForAllImages();
            m_SegmentationReader?.Dispose();
            m_SegmentationReader = null;

            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        void OnBeginCameraRendering(ScriptableRenderContext _, Camera cam)
        {
            if (cam != GetComponent<Camera>())
                return;
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPaused)
                return;
#endif
            ReportAsyncAnnotations();
            CaptureRgbData(cam);
        }

        void OnDisable()
        {
            DatasetCapture.SimulationEnding -= OnSimulationEnding;

            OnSimulationEnding();

            m_ClassLabelingTextureReader?.Dispose();
            m_ClassLabelingTextureReader = null;
            if (segmentationTexture != null)
                segmentationTexture.Release();

            segmentationTexture = null;
            if (labelingTexture != null)
                labelingTexture.Release();

            if (m_RenderedObjectInfoGenerator != null)
            {
                World.DefaultGameObjectInjectionWorld?.GetExistingSystem<GroundTruthLabelSetupSystem>()?.Deactivate(m_RenderedObjectInfoGenerator);
                m_RenderedObjectInfoGenerator?.Dispose();
                m_RenderedObjectInfoGenerator = null;
            }

            if (SensorHandle.IsValid)
                SensorHandle.Dispose();

            SensorHandle = default;

            labelingTexture = null;
        }

        void OnSemanticSegmentationImageRead(int frameCount, NativeArray<short> data)
        {
            var findResult = FindAsyncCaptureInfo(frameCount);
            var asyncCaptureInfo = findResult.asyncCaptureInfo;

            var dxLocalPath = Path.Combine(k_SemanticSegmentationDirectory, k_SegmentationFilePrefix) + frameCount + ".png";
            var path = Path.Combine(Manager.Instance.GetDirectoryFor(k_SemanticSegmentationDirectory), k_SegmentationFilePrefix) + frameCount + ".png";
            var annotation = asyncCaptureInfo.SegmentationAsyncAnnotation;
            if (!annotation.IsValid)
                return;

            annotation.ReportFile(dxLocalPath);

            var asyncRequest = Manager.Instance.CreateRequest<AsyncRequest<AsyncSemanticSegmentationWrite>>();
            asyncRequest.data = new AsyncSemanticSegmentationWrite()
            {
                dataArray = data.ToArray(),
                width = labelingTexture.width,
                height = labelingTexture.height,
                path = path
            };
            asyncRequest.Start((r) =>
            {
                Profiler.EndSample();
                Profiler.BeginSample("Encode");
                var pngBytes = ImageConversion.EncodeArrayToPNG(r.data.dataArray, GraphicsFormat.R8G8B8A8_UNorm, (uint)r.data.width, (uint)r.data.height);
                Profiler.EndSample();
                Profiler.BeginSample("WritePng");
                File.WriteAllBytes(r.data.path, pngBytes);
                Manager.Instance.ConsumerFileProduced(r.data.path);
                Profiler.EndSample();
                return AsyncRequest.Result.Completed;
            });
        }
    }
}
