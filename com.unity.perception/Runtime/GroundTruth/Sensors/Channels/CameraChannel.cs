using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors.Channels
{
    /// <summary>
    /// A <see cref="CameraChannel{T}"/> generates supplementary per-pixel ground truth data
    /// for the pixel data captured by a <see cref="CameraSensor"/>.
    /// </summary>
    /// <note>Derive from this class to define a new channel type to enable on <see cref="CameraSensor"/>s.</note>
    /// <typeparam name="T">
    /// The struct format for the data captured at each pixel of this channel's output texture.
    /// </typeparam>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public abstract class CameraChannel<T> : CameraChannelBase where T : unmanaged
    {
        /// <summary>
        /// Invoked when the channel's output texture is readback during frame capture. The first parameter is the
        /// <see cref="Time.frameCount"/> of the captured frame. The second parameter is the pixel data from the
        /// channel's output texture that was readback from the GPU.
        /// </summary>
        public event Action<int, NativeArray<T>> outputTextureReadback;

        /// <inheritdoc/>
        internal sealed override void InvokeReadbackEvent(CommandBuffer cmd)
        {
            if (outputTextureReadback == null)
                return;

            RenderTextureReader.Capture<T>(cmd, outputTexture,
                (frame, data, texture) => outputTextureReadback.Invoke(frame, data));
        }
    }
}
