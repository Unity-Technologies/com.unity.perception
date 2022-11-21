using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A utility that uses a compute shader to calculate the 2D bounding boxes
    /// of each instance present in an instance segmentation image.
    /// </summary>
    static class RenderedObjectInfoComputer
    {
        /// <summary>
        /// A struct describing the bounding box data of a particular rendered object.
        /// </summary>
        [Serializable]
        struct InstanceBoundingBox
        {
            public uint bottom;
            public uint top;
            public uint left;
            public uint right;
            public uint pixelCount;

            public uint width => right - left + 1;
            public uint height => top - bottom + 1;

            public Rect GetRectBounds(int imageHeight)
            {
                return new Rect(left, imageHeight - top - 1, width, height);
            }
        }

        static readonly int k_PropInstanceIndicesTexture = Shader.PropertyToID("instanceIndicesTexture");
        static readonly int k_PropBoundingBoxBuffer = Shader.PropertyToID("boundingBoxBuffer");
        static ComputeShader s_Shader = ComputeUtilities.LoadShader("CalculateRenderedObjectInfos");

        /// <summary>
        /// Use a compute shader to calculate the 2D bounding boxes
        /// of each instance present in an instance segmentation image.
        /// </summary>
        /// <param name="cmd">The CommandBuffer to enqueue this operation into.</param>
        /// <param name="inputIndicesTexture">
        /// An instance segmentation image for which each pixel contains an unsigned integer who's value is between the
        /// range of zero to the number of labeled objects present in the scene.
        /// </param>
        /// <param name="callback">The callback method to execute after the bounding box data has been calculated.</param>
        public static void CalculateRenderedObjectInfos(
            CommandBuffer cmd, RenderTexture inputIndicesTexture,
            Action<int, NativeArray<RenderedObjectInfo>, SceneHierarchyInformation> callback)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Rendered Object Info Computer")))
            {
                // Make a copy of the current list of instance ids from the LabelManager.
                var instanceIds = new NativeArray<uint>(LabelManager.singleton.instanceIds, Allocator.Persistent);

                // Allocate a new compute buffer to store the bounding box data calculated for each rendered object.
                var instanceBoundingBoxBuffer = new ComputeBuffer(Math.Max(256, instanceIds.Length), sizeof(uint) * 5);

                // Configure the parameters of the compute shader.
                cmd.SetComputeBufferParam(s_Shader, 0, k_PropBoundingBoxBuffer, instanceBoundingBoxBuffer);
                cmd.SetComputeBufferParam(s_Shader, 1, k_PropBoundingBoxBuffer, instanceBoundingBoxBuffer);
                cmd.SetComputeTextureParam(s_Shader, 1, k_PropInstanceIndicesTexture, inputIndicesTexture);

                // Dispatch and execute the compute shader to initialize the compute buffer values.
                var clearThreadGroups = ComputeUtilities.ThreadGroupsCount(instanceBoundingBoxBuffer.count, 256);
                cmd.DispatchCompute(s_Shader, 0, clearThreadGroups, 1, 1);

                // Dispatch and execute the compute shader to calculate each in frame object's pixel count and bounding box.
                var threadGroupsX = ComputeUtilities.ThreadGroupsCount(inputIndicesTexture.width, 16);
                var threadGroupsY = ComputeUtilities.ThreadGroupsCount(inputIndicesTexture.height, 16);
                cmd.DispatchCompute(s_Shader, 1, threadGroupsX, threadGroupsY, 1);

                // Readback the calculated bounding boxes and pixel counts from the GPU.
                var height = inputIndicesTexture.height;
                var instanceIdSet = new HashSet<uint>();

                // Make a copy of the current list of instance ids from the LabelManager.
                ComputeBufferReader.Capture<InstanceBoundingBox>(cmd, instanceBoundingBoxBuffer, (frame, boundsData) =>
                {
                    // Convert the readback GPU instance bounds data into an array of RenderedObjectInfos.
                    var renderedObjectInfos = new NativeList<RenderedObjectInfo>(instanceIds.Length, Allocator.TempJob);
                    for (var instanceIdIndex = 1; instanceIdIndex < instanceIds.Length; instanceIdIndex++)
                    {
                        var instanceBounds = boundsData[instanceIdIndex];
                        if (instanceBounds.pixelCount <= 0)
                            continue;

                        instanceIdSet.Add(instanceIds[instanceIdIndex]);
                        renderedObjectInfos.Add(new RenderedObjectInfo
                        {
                            instanceIndex = (uint)instanceIdIndex,
                            instanceId = instanceIds[instanceIdIndex],
                            boundingBox = instanceBounds.GetRectBounds(height),
                            pixelCount = (int)instanceBounds.pixelCount,
                            instanceColor = LabelManager.singleton.instanceSegmentationColors[instanceIdIndex]
                        });
                    }

                    if (!PerceptionCamera.savedHierarchies.ContainsKey(frame))
                        Debug.LogError($"[SH] Hierarchy NOT found for frame {frame}");

                    var savedHierarchy = PerceptionCamera.savedHierarchies[frame];
                    // Invoke the RenderedObjectInfo callbacks.
                    callback(
                        frame, renderedObjectInfos,
                        // from the full hierarchy, only keeps the instance ids which are visible for
                        // the current perception camera
                        savedHierarchy.FilteredClone(instanceIdSet)
                    );

                    // if no other perception camera is waiting for the hierarchy associated wth this frame
                    // delete the saved hierarchy to save memory
                    savedHierarchy.subscribers -= 1;
                    if (savedHierarchy.subscribers <= 0)
                        PerceptionCamera.savedHierarchies.Remove(frame);

                    // Dispose of allocated resources.
                    renderedObjectInfos.Dispose();
                    instanceIds.Dispose();
                    instanceBoundingBoxBuffer.Release();
                });
            }
        }
    }
}
