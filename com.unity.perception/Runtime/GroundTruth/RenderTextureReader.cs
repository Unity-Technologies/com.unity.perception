using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// RenderTextureReader reads a RenderTexture from the GPU whenever Capture is called
    /// and passes the data back through a provided callback.
    /// </summary>
    static class RenderTextureReader
    {
        static Dictionary<(int, int, GraphicsFormat), Texture2D> s_CachedCpuTextures = new Dictionary<(int, int, GraphicsFormat), Texture2D>();

        /// <summary>
        /// Reads a RenderTexture from the GPU passes the collected data back through a provided callback.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sourceTex"></param>
        /// <param name="imageReadCallback"></param>
        /// <typeparam name="T">The type of the raw texture data to be provided.</typeparam>
        public static void Capture<T>(ScriptableRenderContext context, RenderTexture sourceTex,
            Action<int, NativeArray<T>, RenderTexture> imageReadCallback) where T : struct
        {
            if (PerceptionCamera.useAsyncReadbackIfSupported && SystemInfo.supportsAsyncGPUReadback)
            {
                var commandBuffer = CommandBufferPool.Get("RenderTextureReader");
                var frameCount = Time.frameCount;
                commandBuffer.RequestAsyncReadback(sourceTex,
                    request => OnGpuReadback(request, frameCount, sourceTex, imageReadCallback));
                context.ExecuteCommandBuffer(commandBuffer);
                context.Submit();
                CommandBufferPool.Release(commandBuffer);
            }
            else
            {
                var cpuTexture = GetTextureFromCache(sourceTex.width, sourceTex.height, sourceTex.graphicsFormat);
                RenderTexture.active = sourceTex;
                cpuTexture.ReadPixels(new Rect(0, 0, sourceTex.width, sourceTex.height), 0, 0);
                cpuTexture.Apply();
                RenderTexture.active = null;
                var data = cpuTexture.GetRawTextureData<T>();
                imageReadCallback(Time.frameCount, data, sourceTex);
                data.Dispose();
            }
        }

        /// <summary>
        /// Synchronously wait for all image requests to complete.
        /// </summary>
        public static void WaitForAllImages()
        {
            AsyncGPUReadback.WaitAllRequests();
        }

        static void OnGpuReadback<T>(AsyncGPUReadbackRequest request, int frameCount, RenderTexture sourceTexture,
            Action<int, NativeArray<T>, RenderTexture> imageReadCallback) where T : struct
        {
            if (request.hasError)
            {
                Debug.LogError("Error reading segmentation image from GPU");
            }
            else if (request.done && imageReadCallback != null)
            {
                var pixelData = request.GetData<T>();
                imageReadCallback(frameCount, pixelData, sourceTexture);
                pixelData.Dispose();
            }
        }

        static Texture2D GetTextureFromCache(int width, int height, GraphicsFormat graphicsFormat)
        {
            if (s_CachedCpuTextures.TryGetValue((width, height, graphicsFormat), out var texture))
                return texture;
            var newTexture = new Texture2D(width, height, graphicsFormat, TextureCreationFlags.None);
            s_CachedCpuTextures[(width, height, graphicsFormat)] = newTexture;
            return newTexture;
        }
    }
}
