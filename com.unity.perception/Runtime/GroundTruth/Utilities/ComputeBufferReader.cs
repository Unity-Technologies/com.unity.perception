using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Utilities
{
    /// <summary>
    /// A utility for retrieving compute buffer data from the GPU.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public static class ComputeBufferReader
    {
        /// <summary>
        /// Reads a ComputeBuffer from the GPU and passes the collected data back through a provided callback.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="buffer"></param>
        /// <param name="imageReadCallback"></param>
        /// <typeparam name="T">The type of the raw texture data to be provided.</typeparam>
        public static void Capture<T>(
            CommandBuffer cmd, ComputeBuffer buffer, Action<int, NativeArray<T>> imageReadCallback) where T : struct
        {
            var frameCount = Time.frameCount;
            cmd.RequestAsyncReadback(buffer, request =>
            {
                if (request.hasError)
                {
                    Debug.LogError("Error reading ComputeBuffer from GPU");
                }
                else if (request.done && imageReadCallback != null)
                {
                    var pixelData = request.GetData<T>();
                    imageReadCallback(frameCount, pixelData);
                    pixelData.Dispose();
                }
            });
        }
    }
}
