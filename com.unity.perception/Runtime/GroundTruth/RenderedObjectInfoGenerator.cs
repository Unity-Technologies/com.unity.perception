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
    public class RenderedObjectInfoGenerator : IGroundTruthGenerator, IDisposable
    {
        static ProfilerMarker s_LabelJobs = new ProfilerMarker("Label Jobs");
        static ProfilerMarker s_LabelMerge = new ProfilerMarker("Label Merge");

        const int k_StartingObjectCount = 1 << 8;

        struct Object1DSpan
        {
            public int instanceId;
            public int row;
            public int left;
            public int right;
        }
        [BurstCompile]
        struct ComputeHistogramPerRowJob : IJob
        {
            [ReadOnly]
            public NativeSlice<uint> segmentationImageData;
            public int width;
            public int rows;
            public int rowStart;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<Object1DSpan> boundingBoxes;

            public void Execute()
            {
                for (var row = 0; row < rows; row++)
                {
                    var rowSlice = new NativeSlice<uint>(segmentationImageData, width * row, width);
                    var currentBB = new Object1DSpan
                    {
                        instanceId = -1,
                        row = row + rowStart
                    };
                    for (var i = 0; i < rowSlice.Length; i++)
                    {
                        var value = rowSlice[i];

                        if (value != currentBB.instanceId)
                        {
                            if (currentBB.instanceId > 0)
                            {
                                //save off currentBB
                                currentBB.right = i - 1;
                                boundingBoxes.Add(currentBB);
                            }

                            currentBB = new Object1DSpan
                            {
                                instanceId = (int)value,
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

        NativeList<int> m_InstanceIdToLabelEntryIndexLookup;
        LabelingConfiguration m_LabelingConfiguration;

        // ReSharper disable once InvalidXmlDocComment

        /// <summary>
        /// Create a new CpuRenderedObjectInfoPass with the given LabelingConfiguration.
        /// </summary>
        /// <param name="labelingConfiguration">The LabelingConfiguration to use to determine labelId. Should match the
        /// one used by the <seealso cref="InstanceSegmentationUrpPass"/> generating the input image. See <see cref="Compute"/></param>
        public RenderedObjectInfoGenerator(LabelingConfiguration labelingConfiguration)
        {
            m_LabelingConfiguration = labelingConfiguration;
            m_InstanceIdToLabelEntryIndexLookup = new NativeList<int>(k_StartingObjectCount, Allocator.Persistent);
        }

        /// <inheritdoc/>
        public void SetupMaterialProperties(MaterialPropertyBlock mpb, MeshRenderer meshRenderer, Labeling labeling, uint instanceId)
        {
            if (m_LabelingConfiguration.TryGetMatchingConfigurationEntry(labeling, out var entry, out var index))
            {
                if (m_InstanceIdToLabelEntryIndexLookup.Length <= instanceId)
                {
                    m_InstanceIdToLabelEntryIndexLookup.Resize((int)instanceId + 1, NativeArrayOptions.ClearMemory);
                }

                m_InstanceIdToLabelEntryIndexLookup[(int)instanceId] = index;
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
        /// <param name="perLabelEntryObjectCount">When the method returns, filled with a NativeArray with the count of objects for each entry in <see cref="LabelingConfiguration.LabelEntries"/> in the LabelingConfiguration passed into the constructor.</param>
        /// <param name="allocator">The allocator to use for allocating renderedObjectInfos and perLabelEntryObjectCount.</param>
        public void Compute(NativeArray<uint> instanceSegmentationRawData, int stride, BoundingBoxOrigin boundingBoxOrigin, out NativeArray<RenderedObjectInfo> renderedObjectInfos, out NativeArray<uint> perLabelEntryObjectCount, Allocator allocator)
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
                        segmentationImageData = new NativeSlice<uint>(instanceSegmentationRawData, row * stride, stride * rowsThisJob),
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

            perLabelEntryObjectCount = new NativeArray<uint>(m_LabelingConfiguration.LabelEntries.Count, allocator);
            var boundingBoxMap = new NativeHashMap<int, RenderedObjectInfo>(100, Allocator.Temp);
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
                    var instanceId = keyValueArrays.Keys[i];
                    if (m_InstanceIdToLabelEntryIndexLookup.Length <= instanceId)
                        continue;

                    var labelIndex = m_InstanceIdToLabelEntryIndexLookup[instanceId];
                    var labelId = m_LabelingConfiguration.LabelEntries[labelIndex].id;
                    perLabelEntryObjectCount[labelIndex]++;
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
                        labelId = labelId,
                        boundingBox = boundingBox,
                        pixelCount = renderedObjectInfo.pixelCount
                    };
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

        /// <summary>
        /// Attempts to find the label id for the given instance id using the LabelingConfiguration passed into the constructor.
        /// </summary>
        /// <param name="instanceId">The instanceId of the object for which the labelId should be found</param>
        /// <param name="labelId">The labelId of the object. -1 if not found</param>
        /// <returns>True if a labelId is found for the given instanceId.</returns>
        public bool TryGetLabelIdFromInstanceId(int instanceId, out int labelId)
        {
            labelId = -1;
            if (m_InstanceIdToLabelEntryIndexLookup.Length <= instanceId)
                return false;

            labelId = m_InstanceIdToLabelEntryIndexLookup[instanceId];
            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            m_InstanceIdToLabelEntryIndexLookup.Dispose();
        }
    }
}
