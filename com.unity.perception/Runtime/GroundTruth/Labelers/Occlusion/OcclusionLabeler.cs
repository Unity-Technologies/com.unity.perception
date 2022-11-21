using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// This labeler reports a set of visibility metrics for each object that is visible within the frame.
    /// Each set contains 3 metrics:
    /// <list type="number">
    /// <item>Percent Visible: The portion of an object that visible</item>
    /// <item>Percent In Frame: The portion of an object that is not occluded by the camera frame</item>
    /// <item>Visibility In Frame: The unoccluded portion of the part of an object that is in frame</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// The occlusion labeler operates in the following stages to generate a set of occlusion metrics for
    /// each visible object in the scene:
    /// <list type="number">
    /// <item>Render each object one at a time to capture their in-frame unoccluded pixel count</item>
    /// <item>Render each object one at a time in a cube map while masking out the camera's field-of-view
    ///         to obtain each object's out-of-frame pixel count</item>
    /// <item>Compare these two pixel count values against each object's instance segmentation pixel count
    ///         to obtain the 3 occlusion metrics</item>
    /// </list>
    /// </remarks>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public sealed class OcclusionLabeler : CameraLabeler
    {
        static Material s_SegmentationMaterial;
        static readonly string k_Description = "Visibility metrics for labeled objects";
        static readonly Quaternion[] k_CameraDirections =
        {
            Quaternion.identity,
            Quaternion.Euler(-90, 0, 0),
            Quaternion.Euler(90, 0, 0),
            Quaternion.Euler(0, -90, 0),
            Quaternion.Euler(0, 90, 0),
            Quaternion.Euler(0, 180, 0)
        };
        static readonly string[] k_CameraDirectionNames =
        {
            "Forward",
            "Left",
            "Right",
            "Up",
            "Down",
            "Back"
        };

        struct OcclusionValues
        {
            public uint instanceId;
            public float inFramePixelCount;
            public float outOfFramePixelCount;
        }

        Dictionary<int, AsyncFuture<Metric>> m_AsyncMetrics = new();
        Dictionary<int, NativeArray<uint>> m_InstanceIdsPerFrame = new();
        Dictionary<int, NativeArray<bool>> m_ObjectsToRender = new();
        Dictionary<int, NativeArray<OcclusionValues>> m_ObservablePixelCountsPerFrame = new();

        NativeArray<float> m_PixelWeights;
        OcclusionMetricDefinition m_Definition;
        RenderTexture m_InFrameRT;
        RenderTexture m_InFramePixelWeightsRT;
        RenderTexture m_InFrameMaskedPixelWeightsRT;
        RenderTexture m_CubemapRT;
        RenderTexture m_CubemapPixelWeightsRT;
        RenderTexture m_CubemapObjectWeightsRT;
        RenderTexture m_OutOfFramePixelWeightsRT;

        /// <summary>
        /// The string ID associated with this labeler.
        /// </summary>
        public string metricId = "Occlusion";

        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        public IdLabelConfig idLabelConfig;

        /// <summary>
        /// The <see cref="OcclusionLabeler"/>'s cubemap capture resolution.
        /// Higher resolutions are more expensive to capture and increase the accuracy of calculated visibility metrics.
        /// </summary>
        [Tooltip("Adjust this parameter to control the cubemap resolution this labeler will use when rendering " +
            "objects outside of the camera's field of view. Higher resolutions will increase accuracy at the expense " +
            "of increased computational costs and slower capture frame rates.")]
        [Range(32, 4096)]
        public int outOfFrameResolution = 512;

        ///<inheritdoc/>
        public override string description => k_Description;

        ///<inheritdoc/>
        public override string labelerId => metricId;

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <summary>
        /// An event that is called each frame for which occlusion metrics are calculated.
        /// </summary>
        public event Action<int, NativeArray<OcclusionMetricEntry>> occlusionMetricsComputed;

        /// <summary>
        /// Creates a new OcclusionLabeler.
        /// Be sure to assign <see cref="idLabelConfig"/> before adding to a <see cref="PerceptionCamera"/>.
        /// </summary>
        public OcclusionLabeler() {}

        /// <summary>
        /// Creates a new OcclusionLabeler with an <see cref="IdLabelConfig"/>.
        /// </summary>
        /// <param name="idLabelConfig">The <see cref="IdLabelConfig"/> which associates objects with labels.</param>
        public OcclusionLabeler(IdLabelConfig idLabelConfig)
        {
            if (idLabelConfig == null)
                throw new ArgumentNullException(nameof(idLabelConfig));

            this.idLabelConfig = idLabelConfig;
        }

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
            {
                Debug.LogError("OcclusionLabeler's idLabelConfig field must be assigned.");
                enabled = false;
                return;
            }

            var camera = perceptionCamera.attachedCamera;
            if (camera.orthographic)
            {
                Debug.LogError("OcclusionLabeler does not support orthographic cameras.");
                enabled = false;
                return;
            }

            if (s_SegmentationMaterial == null)
                s_SegmentationMaterial = new(RenderUtilities.LoadPrewarmedShader("Perception/InstanceSegmentation"));

            var channel = perceptionCamera.EnableChannel<InstanceIdChannel>();
            channel.outputTextureReadback += CountPixelsPerObjectInSegmentationTexture;

            m_Definition = new OcclusionMetricDefinition(metricId, k_Description);
            DatasetCapture.RegisterMetric(m_Definition);

            m_PixelWeights = PixelWeightsUtility.CalculatePixelWeightsForSegmentationImage(
                camera.pixelWidth, camera.pixelHeight, camera.fieldOfView);

            // Allocate RenderTextures used for in-frame pixel calculations.
            // These in-frame textures will share the same resolution as the camera to prevent any aliasing
            // issues that could occur when comparing the pixels of an object rendered to two textures with
            // different resolutions.
            m_InFrameRT = new RenderTexture(
                camera.pixelWidth, camera.pixelHeight, 32, GraphicsFormat.R8G8B8A8_UNorm)
            {
                name = "InFrameRT",
                enableRandomWrite = true
            };
            m_InFrameRT.Create();
            m_InFramePixelWeightsRT = PixelWeightsUtility.GeneratePixelWeights(
                camera.pixelWidth, camera.pixelHeight, camera.fieldOfView, camera.aspect);
            m_InFrameMaskedPixelWeightsRT = ComputeUtilities.CreateFloatTexture(camera.pixelWidth, camera.pixelHeight);

            // Allocate RenderTextures used for out-of-frame pixel calculations.
            // These out-of-frame textures will all have square resolutions (width == height)
            // since these textures will process the output of rendered cubemap faces.
            m_CubemapRT = new RenderTexture(
                outOfFrameResolution, outOfFrameResolution, 32, GraphicsFormat.R8G8B8A8_UNorm)
            {
                name = "OutOfFrameRT",
                enableRandomWrite = true
            };
            m_CubemapRT.Create();
            m_CubemapPixelWeightsRT = PixelWeightsUtility.GeneratePixelWeights(outOfFrameResolution, outOfFrameResolution);
            m_CubemapObjectWeightsRT = ComputeUtilities.CreateFloatTexture(outOfFrameResolution, outOfFrameResolution);
            m_OutOfFramePixelWeightsRT = ComputeUtilities.CreateFloatTexture(outOfFrameResolution, outOfFrameResolution);

            visualizationEnabled = supportsVisualization;
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
            if (m_InFrameRT != null)
                m_InFrameRT.Release();
            if (m_InFramePixelWeightsRT != null)
                m_InFramePixelWeightsRT.Release();
            if (m_InFrameMaskedPixelWeightsRT != null)
                m_InFrameMaskedPixelWeightsRT.Release();

            if (m_CubemapRT != null)
                m_CubemapRT.Release();
            if (m_CubemapPixelWeightsRT != null)
                m_CubemapPixelWeightsRT.Release();
            if (m_CubemapObjectWeightsRT != null)
                m_CubemapObjectWeightsRT.Release();
            if (m_OutOfFramePixelWeightsRT != null)
                m_OutOfFramePixelWeightsRT.Release();

            if (m_PixelWeights.IsCreated)
                m_PixelWeights.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
        {
            m_AsyncMetrics[Time.frameCount] = perceptionCamera.SensorHandle.ReportMetricAsync(m_Definition);
        }

        /// <inheritdoc/>
        protected override void OnEndRendering(ScriptableRenderContext ctx)
        {
            // Determine which labelers should have their occlusion metrics calculated.
            var labeledObjectsToRender = LabelManager.singleton.registeredLabels.ToList();

            // Report an empty list of occlusion metrics if there are no labeled objects
            // present in the scene for the current frame.
            if (labeledObjectsToRender.Count == 0)
            {
                ReportNothing();
                return;
            }

            // Identify which labeled objects can be rendered by the occlusion labeler this frame.
            var canRenderObjectCount = 0;
            var objectsToRenderArray = new NativeArray<bool>(labeledObjectsToRender.Count, Allocator.Persistent);
            for (var i = 0; i < labeledObjectsToRender.Count; i++)
            {
                var canRender = CanRenderObject(labeledObjectsToRender[i]);
                objectsToRenderArray[i] = canRender;
                canRenderObjectCount += canRender ? 1 : 0;
            }

            // Report an empty list of occlusion metrics if there are no
            // labeled objects that should be rendered present in the scene.
            if (canRenderObjectCount == 0)
            {
                objectsToRenderArray.Dispose();
                ReportNothing();
                return;
            }

            // Cache array of objects that should be rendered.
            m_ObjectsToRender[Time.frameCount] = objectsToRenderArray;

            // Cache the instance ids for the list of objects to render.
            // These ids will be used to map each calculated pixel counts to a particular object
            // after the pixel counts buffer has been readback from the GPU next frame.
            var instanceIds = new NativeArray<uint>(labeledObjectsToRender.Count, Allocator.Persistent);
            for (var i = 0; i < labeledObjectsToRender.Count; i++)
                instanceIds[i] = labeledObjectsToRender[i].instanceId;
            m_InstanceIdsPerFrame[Time.frameCount] = instanceIds;

            // Obtain a command buffer from the CommandBufferPool.
            var cmd = CommandBufferPool.Get("OcclusionLabeler");

            // Allocate a pixel counts buffer on the GPU and clear it.
            // The first half of this buffer will store the in-frame pixel count for each labeled object.
            // The second half of this buffer will store the out-of-frame pixel count for each labeled object.
            var pixelCountsBuffer = new ComputeBuffer(labeledObjectsToRender.Count * 2, sizeof(float));
            ClearUtility.ClearFloatBuffer(cmd, pixelCountsBuffer, 0f);

            // Calculate the observable surface area within the camera frame occupied by labeled objects.
            var camera = perceptionCamera.attachedCamera;
            using (new ProfilingScope(cmd, new ProfilingSampler("In Frame Visibility")))
            {
                // Set the projection matrix to the actual camera's projection matrix.
                cmd.SetProjectionMatrix(camera.projectionMatrix);

                // Set the view matrix to be equivalent to the actual camera.
                cmd.SetViewMatrix(camera.worldToCameraMatrix);

                for (var i = 0; i < labeledObjectsToRender.Count; i++)
                {
                    var labeledObject = labeledObjectsToRender[i];
                    if (!objectsToRenderArray[i])
                        continue;

                    using (new ProfilingScope(cmd, new ProfilingSampler($"Instance ID: {labeledObject.instanceId}")))
                    {
                        // Clear the RenderTexture of the last object's pixels.
                        cmd.SetRenderTarget(m_InFrameRT);
                        cmd.ClearRenderTarget(true, true, Color.clear);

                        // Render the labeled object to the blank RenderTexture.
                        RecursivelyAppendChildObjectsToRenderQueue(cmd, labeledObject.gameObject);

                        // Zero out all weights for pixels that are not occupied by an object.
                        MaskUtility.MaskFloatTexture(
                            cmd, m_InFramePixelWeightsRT, m_InFrameRT, m_InFrameMaskedPixelWeightsRT);

                        // Calculate the sum of all the remaining in-frame pixel weights
                        // and write this sum to the pixelCountsBuffer.
                        SumUtility.SumFloatTexture(cmd, m_InFrameMaskedPixelWeightsRT, pixelCountsBuffer, i);
                    }
                }
            }

            // Create a 90 degree fov camera projection matrix for cubemap capture.
            cmd.SetProjectionMatrix(Matrix4x4.Perspective(90f, 1.0f, camera.nearClipPlane, camera.farClipPlane));

            var bufferOffset = pixelCountsBuffer.count / 2;
            var transform = camera.transform;
            var fov = camera.fieldOfView;
            var aspect = camera.aspect;

            // Calculate the observable surface area outside of the camera frame occupied by labeled objects.
            for (var i = 0; i < k_CameraDirections.Length; i++)
            {
                using (new ProfilingScope(cmd, new ProfilingSampler($"Cubemap face: {k_CameraDirectionNames[i]}")))
                {
                    // Calculate a new view matrix to "rotate" the virtual camera in the direction to capture.
                    var cameraDirection = k_CameraDirections[i];
                    var viewMatrix = Matrix4x4.TRS(
                        transform.position, transform.rotation * cameraDirection, transform.lossyScale);
                    var scaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
                    cmd.SetViewMatrix(scaleMatrix * viewMatrix.inverse);

                    for (var j = 0; j < labeledObjectsToRender.Count; j++)
                    {
                        var labeledObject = labeledObjectsToRender[j];
                        if (!objectsToRenderArray[j])
                            continue;

                        using (new ProfilingScope(cmd, new ProfilingSampler($"Instance ID: {labeledObject.instanceId}")))
                        {
                            // Render the object to a blank RenderTexture.
                            cmd.SetRenderTarget(m_CubemapRT);
                            cmd.ClearRenderTarget(true, true, Color.clear);
                            RecursivelyAppendChildObjectsToRenderQueue(cmd, labeledObject.gameObject);

                            // Zero out all weights for pixels that are not occupied by an object.
                            MaskUtility.MaskFloatTexture(
                                cmd, m_CubemapPixelWeightsRT, m_CubemapRT, m_CubemapObjectWeightsRT);

                            // Exclude pixels that fall within the actual camera's field of view.
                            FovMaskUtility.MaskByFov(
                                cmd, m_CubemapObjectWeightsRT, m_OutOfFramePixelWeightsRT, fov, aspect, i);

                            // Calculate the sum of all the remaining out-of-frame pixel weights
                            // and write this sum to the pixelCountsBuffer.
                            SumUtility.SumFloatTexture(
                                cmd, m_OutOfFramePixelWeightsRT, pixelCountsBuffer, j + bufferOffset);
                        }
                    }
                }
            }

            // Switch the render target back to the back buffer.
            cmd.SetRenderTarget((RenderTexture)null);

            // Readback pixel count data from the GPU.
            ComputeBufferReader.Capture<float>(cmd, pixelCountsBuffer, (frame, occlusionValues) =>
            {
                pixelCountsBuffer.Release();
                var cachedInstanceIds = m_InstanceIdsPerFrame[frame];
                m_InstanceIdsPerFrame.Remove(frame);

                // Combine labeled object instance id keys and pixel count values into one NativeHashMap.
                var observablePixelsPerObject = new NativeArray<OcclusionValues>(
                    cachedInstanceIds.Length, Allocator.Persistent);
                for (var i = 0; i < cachedInstanceIds.Length; i++)
                {
                    observablePixelsPerObject[i] = new OcclusionValues
                    {
                        instanceId = cachedInstanceIds[i],
                        inFramePixelCount = occlusionValues[i],
                        outOfFramePixelCount = occlusionValues[i + cachedInstanceIds.Length]
                    };
                }
                cachedInstanceIds.Dispose();

                // Cache this frame's observablePixelCounts for processing that will occur after the
                // instance segmentation image has been asynchronously readback from the GPU.
                m_ObservablePixelCountsPerFrame[frame] = observablePixelsPerObject;
            });

            // Enqueue the CommandBuffer for execution.
            ctx.ExecuteCommandBuffer(cmd);
        }

        void ReportNothing()
        {
            var emptyPixelCountsMap = new NativeArray<float>(0, Allocator.Persistent);
            var emptyOcclusionValuesMap = new NativeArray<OcclusionValues>(0, Allocator.Persistent);
            ReportOcclusionMetrics(Time.frameCount, emptyPixelCountsMap, emptyOcclusionValuesMap);
            emptyPixelCountsMap.Dispose();
            emptyOcclusionValuesMap.Dispose();
        }

        bool CanRenderObject(Labeling labeledObject)
        {
            // The occlusion labeler only supports object's with Renderer components.
            // For example, Terrain components do not use a Renderer component for rendering so
            // terrain objects are not supported by the occlusion labeler.
            return labeledObject.GetComponentInChildren<Renderer>() != null;
        }

        void RecursivelyAppendChildObjectsToRenderQueue(CommandBuffer commandBuffer, GameObject gameObject)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
                commandBuffer.DrawRenderer(renderer, s_SegmentationMaterial);
            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                if (child.GetComponent<Labeling>() != null)
                    continue;
                RecursivelyAppendChildObjectsToRenderQueue(commandBuffer, child);
            }
        }

        void CountPixelsPerObjectInSegmentationTexture(int frame, NativeArray<uint> pixelData)
        {
            // Lookup observable pixel counts from the given frame.
            if (!m_ObservablePixelCountsPerFrame.TryGetValue(frame, out var occlusionValuesMap))
                return;
            m_ObservablePixelCountsPerFrame.Remove(frame);

            // Lookup where each labeled object from the given frame was supposed to be rendered.
            if (!m_ObjectsToRender.TryGetValue(frame, out var objectsToRender))
                return;
            m_ObjectsToRender.Remove(frame);

            // Count the weighted sum of pixels occupied by each object in the instance segmentation image.
            var visiblePixelsPerObject = PixelWeightsUtility.WeightedPixelCountsById(
                pixelData, m_PixelWeights, objectsToRender.Length);
            for (var i = 0; i < objectsToRender.Length; i++)
                if (!objectsToRender[i])
                    visiblePixelsPerObject[i] = 0f;

            ReportOcclusionMetrics(frame, visiblePixelsPerObject, occlusionValuesMap);

            objectsToRender.Dispose();
            visiblePixelsPerObject.Dispose();
            occlusionValuesMap.Dispose();
        }

        void ReportOcclusionMetrics(
            int frameCount,
            NativeArray<float> visiblePixelCounts,
            NativeArray<OcclusionValues> occlusionValuesMap)
        {
            // Calculate object visibility metrics from visible and observable pixel counts.
            var messages = new NativeList<OcclusionMetricEntry>(8, Allocator.Temp);
            if (visiblePixelCounts.Length > 0)
            {
                for (var i = 0; i < occlusionValuesMap.Length; i++)
                {
                    var visiblePixelsCount = visiblePixelCounts[i];
                    if (visiblePixelCounts[i] <= 0f)
                        continue;

                    var occlusionValues = occlusionValuesMap[i];
                    var instanceId = occlusionValues.instanceId;
                    var observablePixelCount = occlusionValues.outOfFramePixelCount + occlusionValues.inFramePixelCount;
                    var visibility = Mathf.Clamp01(visiblePixelsCount / observablePixelCount);
                    var visibilityInFrame = Mathf.Clamp01(visiblePixelsCount / occlusionValues.inFramePixelCount);
                    var portionInFrame = Mathf.Clamp01(occlusionValues.inFramePixelCount / observablePixelCount);

                    messages.Add(new OcclusionMetricEntry
                    {
                        instanceID = instanceId,
                        percentVisible = visibility,
                        percentInFrame = portionInFrame,
                        visibilityInFrame = visibilityInFrame
                    });
                }
            }

            // Alert this labeler's subscribers that the occlusion metrics have been calculated for this frame.
            occlusionMetricsComputed?.Invoke(frameCount, messages);

            // Convert native metric data to a managed list of generic messages for dataset reporting.
            var genericMessages = new IMessageProducer[messages.Length];
            for (var i = 0; i < messages.Length; i++)
                genericMessages[i] = messages[i];

            // Report the occlusion metrics to the dataset consumer.
            var payload = new GenericMetric(genericMessages, m_Definition, perceptionCamera.id);
            var metric = m_AsyncMetrics[frameCount];
            m_AsyncMetrics.Remove(frameCount);
            metric.Report(payload);

            // Visualize the calculated occlusion metrics.
            VisualizeMetrics(messages);

            messages.Dispose();
        }

        void VisualizeMetrics(NativeArray<OcclusionMetricEntry> occlusionMetrics)
        {
            if (!perceptionCamera.showVisualizations)
                return;

            // Clear the existing occlusion metric visualizations.
            hudPanel.RemoveEntries(this);

            if (visualizationEnabled)
            {
                const int maxEntries = 10;

                // Map the first 10 occlusion metrics to their instance ID to accelerate the object name lookups.
                var metricsMap = new Dictionary<uint, OcclusionMetricEntry>();
                for (var i = 0; i < occlusionMetrics.Length && i < maxEntries; i++)
                {
                    var message = occlusionMetrics[i];
                    metricsMap.Add(message.instanceID, message);
                }

                // Visualize the first 10 visibility metrics.
                var foundLabels = 0;
                var labels = LabelManager.singleton.registeredLabels;
                foreach (var label in labels)
                {
                    if (!metricsMap.TryGetValue(label.instanceId, out var metrics))
                        continue;

                    hudPanel.UpdateEntry(this, $"{label.name}", $"{metrics.percentVisible:P}");

                    if (++foundLabels >= maxEntries)
                        break;
                }

                // Show the count of un-visualized metrics if there are too many reported occlusion metrics.
                if (occlusionMetrics.Length > maxEntries)
                    hudPanel.UpdateEntry(this, $"+ {occlusionMetrics.Length - maxEntries} more ...", string.Empty);
            }
        }
    }
}
