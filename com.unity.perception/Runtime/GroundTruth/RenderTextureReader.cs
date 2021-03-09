using System;
using System.Linq;
using Unity.Collections;
using Unity.Simulation;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// RenderTextureReader reads a RenderTexture from the GPU whenever Capture is called and passes the data back through a provided callback.
    /// </summary>
    /// <typeparam name="T">The type of the raw texture data to be provided.</typeparam>
    class RenderTextureReader<T> : IDisposable where T : struct
    {
        RenderTexture m_Source;
        Texture2D m_CpuTexture;

        /// <summary>
        /// Creates a new <see cref="RenderTextureReader{T}"/> for the given <see cref="RenderTexture"/>, <see cref="Camera"/>, and image readback callback
        /// </summary>
        /// <param name="source">The <see cref="RenderTexture"/> to read from.</param>
        public RenderTextureReader(RenderTexture source)
        {
            m_Source = source;
        }

        public void Capture(ScriptableRenderContext context, Action<int, NativeArray<T>, RenderTexture> imageReadCallback)
        {
            if (!GraphicsUtilities.SupportsAsyncReadback())
            {
                RenderTexture.active = m_Source;

                if (m_CpuTexture == null)
                    m_CpuTexture = new Texture2D(m_Source.width, m_Source.height, m_Source.graphicsFormat, TextureCreationFlags.None);

                m_CpuTexture.ReadPixels(new Rect(
                    Vector2.zero,
                    new Vector2(m_Source.width, m_Source.height)),
                    0, 0);
                RenderTexture.active = null;
                var data = m_CpuTexture.GetRawTextureData<T>();
                imageReadCallback(Time.frameCount, data, m_Source);
            }
            else
            {
                var commandBuffer = CommandBufferPool.Get("RenderTextureReader");
                var frameCount = Time.frameCount;
                commandBuffer.RequestAsyncReadback(m_Source, r => OnGpuReadback(r, frameCount, imageReadCallback));
                context.ExecuteCommandBuffer(commandBuffer);
                context.Submit();
                CommandBufferPool.Release(commandBuffer);
            }
        }

        void OnGpuReadback(AsyncGPUReadbackRequest request, int frameCount,
            Action<int, NativeArray<T>, RenderTexture> imageReadCallback)
        {
            if (request.hasError)
            {
                Debug.LogError("Error reading segmentation image from GPU");
            }
            else if (request.done && imageReadCallback != null)
            {
                imageReadCallback(frameCount, request.GetData<T>(), m_Source);
            }
        }

        /// <summary>
        /// Synchronously wait for all image requests to complete.
        /// </summary>
        public void WaitForAllImages()
        {
            AsyncGPUReadback.WaitAllRequests();
        }

        /// <summary>
        /// Shut down the reader, waiting for all requests to return.
        /// </summary>
        public void Dispose()
        {
            WaitForAllImages();
            if (m_CpuTexture != null)
            {
                Object.Destroy(m_CpuTexture);
                m_CpuTexture = null;
            }
        }
    }
}
