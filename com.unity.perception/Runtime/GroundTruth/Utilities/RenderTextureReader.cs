using System;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Utilities
{
    /// <summary>
    /// RenderTextureReader reads a RenderTexture from the GPU whenever Capture is called
    /// and passes the data back through a provided callback.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public static class RenderTextureReader
    {
        /// <summary>
        /// Reads a RenderTexture from the GPU passes the collected data back through a provided callback.
        /// </summary>
        /// <param name="cmd">The CommandBuffer to enqueue the readback operation to.</param>
        /// <param name="sourceTex">The RenderTexture to readback.</param>
        /// <param name="imageReadCallback">The callback method to execute once the texture has been readback.</param>
        /// <typeparam name="T">The type of the raw texture data to be provided.</typeparam>
        public static void Capture<T>(CommandBuffer cmd, RenderTexture sourceTex,
            Action<int, NativeArray<T>, RenderTexture> imageReadCallback) where T : struct
        {
            cmd.BeginSample("Readback RenderTexture");
            if (sourceTex.graphicsFormat == GraphicsFormat.R32_UInt)
            {
                var buffer = CopyUtility.CopyUIntTextureToBuffer(cmd, sourceTex);
                ComputeBufferReader.Capture<uint>(cmd, buffer, (frame, data) =>
                {
                    imageReadCallback(frame, data.Reinterpret<T>(sizeof(uint)), sourceTex);
                    buffer.Release();
                });
            }
            else if (sourceTex.graphicsFormat == GraphicsFormat.R32_SFloat)
            {
                var buffer = CopyUtility.CopyFloatTextureToBuffer(cmd, sourceTex);
                ComputeBufferReader.Capture<float>(cmd, buffer, (frame, data) =>
                {
                    imageReadCallback(frame, data.Reinterpret<T>(sizeof(float)), sourceTex);
                    buffer.Release();
                });
            }
            else
            {
                var frame = Time.frameCount;
                cmd.RequestAsyncReadback(sourceTex, request =>
                {
                    if (request.hasError)
                    {
                        Debug.LogError($"Error reading RenderTexture \"{sourceTex.name}\" from GPU");
                    }
                    else if (request.done && imageReadCallback != null)
                    {
                        var pixelData = request.GetData<T>();
                        imageReadCallback(frame, pixelData, sourceTex);
                        pixelData.Dispose();
                    }
                });
            }
            cmd.EndSample("Readback RenderTexture");
        }
    }
}
