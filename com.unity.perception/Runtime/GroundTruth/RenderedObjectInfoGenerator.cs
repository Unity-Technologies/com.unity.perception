using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A CPU-based pass which computes bounding box and pixel counts per-object from instance segmentation images
    /// </summary>
    public class RenderedObjectInfoGenerator
    {
        static ProfilerMarker s_LabelJobs = new ProfilerMarker("Label Jobs");
        static ProfilerMarker s_LabelMerge = new ProfilerMarker("Label Merge");

        struct Object1DSpan
        {
            public uint instanceId;
            public int row;
            public int left;
            public int right;
        }
        [BurstCompile]
        struct ComputeHistogramPerRowJob : IJob
        {
            [ReadOnly]
            public NativeSlice<Color32> segmentationImageData;
            public int width;
            public int rows;
            public int rowStart;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<Object1DSpan> boundingBoxes;

            public void Execute()
            {
                for (var row = 0; row < rows; row++)
                {
                    var rowSlice = new NativeSlice<Color32>(segmentationImageData, width * row, width);

                    var currentBB = new Object1DSpan
                    {
                        instanceId = 0,
                        row = row + rowStart
                    };
                    for (var i = 0; i < rowSlice.Length; i++)
                    {
                        var packed = InstanceIdToColorMapping.GetPackedColorFromColor(rowSlice[i]);
                        // pixel color black (0,0,0,255) is reserved for no hit, so set it to id 0
                        var id = packed == 255 ? 0 : packed;

                        if (id != currentBB.instanceId)
                        {
                            if (currentBB.instanceId > 0)
                            {
                                //save off currentBB
                                currentBB.right = i - 1;
                                boundingBoxes.Add(currentBB);
                            }

                            currentBB = new Object1DSpan
                            {
                                instanceId = id,
                                left = i,
                                row = row + rowStart
                            };
                        }
                    }

                    if (currentBB.instanceId > 0)
                    {
                        //save off currentBB
                        currentBB.right = width - 1;
                        boundingBoxes.Add(currentBB);
                    }
                }
            }
        }

        // ReSharper disable once InvalidXmlDocComment

        /// <summary>
        /// Compute RenderedObjectInfo for each visible object in the given instance segmentation image.
        /// InstanceSegmentationRawData should be the raw data from a texture filled by <see cref="InstanceSegmentationUrpPass"/> or  <see cref="InstanceSegmentationPass"/>
        /// using the same LabelingConfiguration that was passed into this object.
        /// </summary>
        /// <param name="instanceSegmentationRawData">The raw instance segmentation image.</param>
        /// <param name="stride">Stride of the image data. Should be equal to the width of the image.</param>
        /// <param name="boundingBoxOrigin">Whether bounding boxes should be top-left or bottom-right-based.</param>
        /// <param name="renderedObjectInfos">When this method returns, filled with RenderedObjectInfo entries for each object visible in the frame.</param>
        /// <param name="allocator">The allocator to use for allocating renderedObjectInfos and perLabelEntryObjectCount.</param>
        public void Compute(NativeArray<Color32> instanceSegmentationRawData, int stride, BoundingBoxOrigin boundingBoxOrigin, out NativeArray<RenderedObjectInfo> renderedObjectInfos, Allocator allocator)
        {
            const int jobCount = 24;
            var height = instanceSegmentationRawData.Length / stride;
            //special math to round up
            var rowsPerJob = height / jobCount;
            var rowRemainder = height % jobCount;
            var handles = new NativeArray<JobHandle>(jobCount, Allocator.Temp);
            var jobBoundingBoxLists = new NativeList<Object1DSpan>[jobCount];
            using (s_LabelJobs.Auto())
            {
                for (int row = 0, jobIndex = 0; row < height; row += rowsPerJob, jobIndex++)
                {
                    jobBoundingBoxLists[jobIndex] = new NativeList<Object1DSpan>(10, Allocator.TempJob);
                    var rowsThisJob = math.min(height - row, rowsPerJob);
                    if (jobIndex < rowRemainder)
                        rowsThisJob++;

                    handles[jobIndex] = new ComputeHistogramPerRowJob
                    {
                        segmentationImageData = new NativeSlice<Color32>(instanceSegmentationRawData, row * stride, stride * rowsThisJob),
                        width = stride,
                        rowStart = row,
                        rows = rowsThisJob,
                        boundingBoxes = jobBoundingBoxLists[jobIndex]
                    }.Schedule();

                    if (jobIndex < rowRemainder)
                        row++;
                }

                JobHandle.CompleteAll(handles);
            }

            var boundingBoxMap = new NativeHashMap<uint, RenderedObjectInfo>(100, Allocator.Temp);
            using (s_LabelMerge.Auto())
            {
                foreach (var boundingBoxList in jobBoundingBoxLists)
                {
                    if (!boundingBoxList.IsCreated)
                        continue;

                    foreach (var info1D in boundingBoxList)
                    {
                        var objectInfo = new RenderedObjectInfo
                        {
                            boundingBox = new Rect(info1D.left, info1D.row, info1D.right - info1D.left + 1, 1),
                            instanceId = info1D.instanceId,
                            pixelCount = info1D.right - info1D.left + 1
                        };

                        if (boundingBoxMap.TryGetValue(info1D.instanceId, out var info))
                        {
                            objectInfo.boundingBox = Rect.MinMaxRect(
                                math.min(info.boundingBox.xMin, objectInfo.boundingBox.xMin),
                                math.min(info.boundingBox.yMin, objectInfo.boundingBox.yMin),
                                math.max(info.boundingBox.xMax, objectInfo.boundingBox.xMax),
                                math.max(info.boundingBox.yMax, objectInfo.boundingBox.yMax));
                            objectInfo.pixelCount += info.pixelCount;
                        }

                        boundingBoxMap[info1D.instanceId] = objectInfo;
                    }
                }

                var keyValueArrays = boundingBoxMap.GetKeyValueArrays(Allocator.Temp);
                renderedObjectInfos = new NativeArray<RenderedObjectInfo>(keyValueArrays.Keys.Length, allocator);
                for (var i = 0; i < keyValueArrays.Keys.Length; i++)
                {
                    var color = InstanceIdToColorMapping.GetColorFromPackedColor(keyValueArrays.Keys[i]);
                    if (InstanceIdToColorMapping.TryGetInstanceIdFromColor(color, out var instanceId))
                    {
                        var renderedObjectInfo = keyValueArrays.Values[i];
                        var boundingBox = renderedObjectInfo.boundingBox;
                        if (boundingBoxOrigin == BoundingBoxOrigin.TopLeft)
                        {
                            var y = height - boundingBox.yMax;
                            boundingBox = new Rect(boundingBox.x, y, boundingBox.width, boundingBox.height);
                        }

                        renderedObjectInfos[i] = new RenderedObjectInfo
                        {
                            instanceId = instanceId,
                            boundingBox = boundingBox,
                            pixelCount = renderedObjectInfo.pixelCount,
                            instanceColor = color
                        };
                    }
                    else
                    {
                        Debug.LogError($"Could not generate instance ID for object, ID exceeded maximum ID");
                    }
                }
                keyValueArrays.Dispose();
            }

            boundingBoxMap.Dispose();
            foreach (var rowBoundingBox in jobBoundingBoxLists)
            {
                if (rowBoundingBox.IsCreated)
                    rowBoundingBox.Dispose();
            }

            handles.Dispose();
        }
    }
}
