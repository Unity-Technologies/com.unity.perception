using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Utilities
{
    /// <summary>
    /// A utility for disposing of compute buffers after they have been used within the current frame.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    static class ComputeBufferDisposer
    {
        static Queue<(int, ComputeBuffer)> s_DisposedBuffers = new Queue<(int, ComputeBuffer)>();

        static ComputeBufferDisposer()
        {
            Application.quitting += () =>
            {
                while (s_DisposedBuffers.Count > 0)
                {
                    var pair = s_DisposedBuffers.Dequeue();
                    pair.Item2.Release();
                }
            };
        }

        /// <summary>
        /// Enqueues a ComputeBuffer to be released after the current frame has completed.
        /// </summary>
        /// <param name="buffer">The ComputeBuffer to release.</param>
        public static void ReleaseAfterCurrentFrame(ComputeBuffer buffer)
        {
            s_DisposedBuffers.Enqueue((Time.frameCount, buffer));
        }

        internal static void ReleaseExpiredBuffers()
        {
            if (s_DisposedBuffers.Count == 0)
                return;

            var currentFrame = Time.frameCount;
            var pair = s_DisposedBuffers.Dequeue();
            while (pair.Item1 < currentFrame)
            {
                pair.Item2.Release();
                if (s_DisposedBuffers.Count == 0)
                    break;
                pair = s_DisposedBuffers.Dequeue();
            }
        }
    }
}
