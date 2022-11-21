using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Utilities
{
    /// <summary>
    /// A utility for encoding raw pixel data into a variety of formats using the Unity Job System.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public static class ImageEncoder
    {
        static JobHandle s_EncodingJobs;
        static int s_JobCounter;
        static Thread s_MainThread;
        static Dictionary<int, PendingEncodedDataCallback> s_PendingCallbacks = new Dictionary<int, PendingEncodedDataCallback>();

        [RuntimeInitializeOnLoadMethod]
        static void SetMainThreadId()
        {
            s_MainThread = Thread.CurrentThread;
        }

        static bool IsMainThread()
        {
            return s_MainThread != null && Thread.CurrentThread == s_MainThread;
        }

        /// <summary>
        /// Whether to encode images in parallel (using the JobSystem), or to synchronously encode images.
        /// </summary>
        public static bool encodeImagesAsynchronously { get; set; } = true;

        /// <summary>
        /// Wait for all in flight encoding jobs to complete, along with their callbacks.
        /// This API can only be called from the main thread.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static void WaitForAllEncodingJobsToComplete()
        {
            if (!IsMainThread())
                throw new InvalidOperationException(
                    "Waiting for encoding jobs to complete can only be accomplished on the main thread.");
            s_EncodingJobs.Complete();
            ExecutePendingCallbacks();
        }

        /// <summary>
        /// Executes callbacks for completed encoding jobs.
        /// All callbacks will be executed on the main thread.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        internal static void ExecutePendingCallbacks()
        {
            if (!IsMainThread())
                throw new InvalidOperationException("Pending callbacks can only be executed on the main thread.");

            var completedCallbacks = new List<int>();
            foreach (var index in s_PendingCallbacks.Keys)
            {
                var callback = s_PendingCallbacks[index];
                if (callback.jobHandle.IsCompleted)
                {
                    callback.jobHandle.Complete();
                    callback.ExecuteCallback();
                    completedCallbacks.Add(index);
                }
            }

            foreach (var completedHandle in completedCallbacks)
                s_PendingCallbacks.Remove(completedHandle);
        }

        /// <summary>
        /// Encodes raw pixel data asynchronously.
        /// The callback assigned to this method however will be executed on the main thread.
        /// </summary>
        /// <typeparam name="T"> Where T is a pixel suitable format for ImageConversion.EncodeNativeArrayToJPG </typeparam>
        /// <param name="rawImageData">A native buffer of raw pixel data.</param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="graphicsFormat"></param>
        /// <param name="encodingFormat">The lossless file format to encode the image to (RAW, PNG, or EXR)</param>
        /// <param name="callback">An action to perform (on the main thread) after the asynchronous encoding job has completed.</param>
        public static void EncodeImage<T>(NativeArray<T> rawImageData, int width, int height,
            GraphicsFormat graphicsFormat, LosslessImageEncodingFormat encodingFormat,
            Action<NativeArray<byte>> callback) where T : unmanaged
        {
            EncodeImage(rawImageData, width, height, graphicsFormat, ConvertFormat(encodingFormat), callback);
        }

        /// <summary>
        /// Encodes raw pixel data asynchronously.
        /// The callback assigned to this method however will be executed on the main thread.
        /// </summary>
        /// <typeparam name="T"> Where T is a pixel suitable format for ImageConversion.EncodeNativeArrayToJPG </typeparam>
        /// <param name="rawImageData">A native buffer of raw pixel data.</param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="graphicsFormat"></param>
        /// <param name="encodingFormat">The file format to encode the image to (example: JPEG, PNG, etc.)</param>
        /// <param name="callback">An action to perform (on the main thread) after the asynchronous encoding job has completed.</param>
        public static void EncodeImage<T>(NativeArray<T> rawImageData, int width, int height,
            GraphicsFormat graphicsFormat, ImageEncodingFormat encodingFormat,
            Action<NativeArray<byte>> callback) where T : unmanaged
        {
            if (encodingFormat == ImageEncodingFormat.Raw)
            {
                callback.Invoke(rawImageData.Reinterpret<byte>(SizeOfStruct<T>()));
            }
            else if (encodeImagesAsynchronously)
            {
                // Copy the raw image data to ensure the encoding job doesn't potentially access deallocated memory.
                rawImageData = new NativeArray<T>(rawImageData, Allocator.Persistent);

                Profiler.BeginSample("Queuing Encoding Worker");
                var encodedData = new NativeList<byte>(Allocator.Persistent);
                var encodeJob = new EncodeJob<T>
                {
                    data = rawImageData,
                    width = width,
                    height = height,
                    graphicsFormat = graphicsFormat,
                    encodingFormat = encodingFormat,
                    encodedData = encodedData
                }.Schedule();
                rawImageData.Dispose(encodeJob);

                s_PendingCallbacks.Add(s_JobCounter++, new PendingEncodedDataCallback
                {
                    jobHandle = encodeJob,
                    encodedData = encodedData,
                    callback = callback
                });

                s_EncodingJobs = JobHandle.CombineDependencies(s_EncodingJobs, encodeJob);
                Profiler.EndSample();
            }
            else
            {
                Profiler.BeginSample("Encoding Image");
                var encodedData = EncodeImageData(rawImageData, width, height, graphicsFormat, encodingFormat);
                Profiler.EndSample();

                callback.Invoke(encodedData);
                encodedData.Dispose();
            }
        }

        /// <summary>
        /// Converts a <see cref="LosslessImageEncodingFormat"/> enum to an <see cref="ImageEncodingFormat"/> enum.
        /// </summary>
        /// <param name="encodingFormat">The file format value to convert.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static ImageEncodingFormat ConvertFormat(LosslessImageEncodingFormat encodingFormat)
        {
            switch (encodingFormat)
            {
                case LosslessImageEncodingFormat.Raw:
                    return ImageEncodingFormat.Raw;
                case LosslessImageEncodingFormat.Png:
                    return ImageEncodingFormat.Png;
                case LosslessImageEncodingFormat.Exr:
                    return ImageEncodingFormat.Exr;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encodingFormat), encodingFormat, null);
            }
        }

        /// <summary>
        /// Converts an <see cref="ImageEncodingFormat"/> enum to a <see cref="LosslessImageEncodingFormat"/> enum.
        /// </summary>
        /// <param name="encodingFormat">The file format value to convert.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static LosslessImageEncodingFormat ConvertFormat(ImageEncodingFormat encodingFormat)
        {
            switch (encodingFormat)
            {
                case ImageEncodingFormat.Raw:
                    return LosslessImageEncodingFormat.Raw;
                case ImageEncodingFormat.Png:
                    return LosslessImageEncodingFormat.Png;
                case ImageEncodingFormat.Exr:
                    return LosslessImageEncodingFormat.Exr;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encodingFormat), encodingFormat, null);
            }
        }

        static NativeArray<byte> EncodeImageData<T>(NativeArray<T> data, int width, int height,
            GraphicsFormat format, ImageEncodingFormat encodingFormat) where T : unmanaged
        {
            NativeArray<byte> output;
            switch (encodingFormat)
            {
                case ImageEncodingFormat.Raw:
                    throw new InvalidOperationException("Raw image data doesn't need encoding");
                case ImageEncodingFormat.Jpg:
                    output = ImageConversion.EncodeNativeArrayToJPG(
                        data, format, (uint)width, (uint)height, quality: 100);
                    break;
                case ImageEncodingFormat.Png:
                    output = ImageConversion.EncodeNativeArrayToPNG(
                        data, format, (uint)width, (uint)height);
                    break;
                case ImageEncodingFormat.Exr:
                    var flags = Texture2D.EXRFlags.CompressZIP;
                    var bitDepth = ChannelBitDepth(format);
                    if (bitDepth == 32)
                        flags |= Texture2D.EXRFlags.OutputAsFloat;
                    output = ImageConversion.EncodeNativeArrayToEXR(
                        data, format, (uint)width, (uint)height, flags: flags);
                    break;
                default:
                    throw new InvalidOperationException();
            }
            return output;
        }

        static int ChannelBitDepth(GraphicsFormat format)
        {
            switch (format)
            {
                case GraphicsFormat.R8G8B8A8_SRGB:
                    return 8;
                case GraphicsFormat.R8G8B8A8_UNorm:
                    return 8;
                case GraphicsFormat.R16_UNorm:
                    return 16;
                case GraphicsFormat.R16_SFloat:
                    return 16;
                case GraphicsFormat.R16G16B16A16_UNorm:
                    return 16;
                case GraphicsFormat.R16G16B16A16_SFloat:
                    return 16;
                case GraphicsFormat.R32_SFloat:
                    return 32;
                case GraphicsFormat.R32G32B32A32_SFloat:
                    return 32;
                default:
                    throw new InvalidOperationException(
                        $"The image encoder does not currently support the graphics format {format}.");
            }
        }

        static int SizeOfStruct<T>() where T : unmanaged
        {
            unsafe
            {
                return sizeof(T);
            }
        }

        struct PendingEncodedDataCallback
        {
            public JobHandle jobHandle;
            public NativeList<byte> encodedData;
            public Action<NativeArray<byte>> callback;

            public void ExecuteCallback()
            {
                callback?.Invoke(encodedData);
                encodedData.Dispose();
            }
        }

        struct EncodeJob<T> : IJob where T : unmanaged
        {
            public int width;
            public int height;
            public GraphicsFormat graphicsFormat;
            public NativeArray<T> data;
            public ImageEncodingFormat encodingFormat;

            [NativeDisableContainerSafetyRestriction]
            public NativeList<byte> encodedData;

            public void Execute()
            {
                var output = EncodeImageData(data, width, height, graphicsFormat, encodingFormat);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, AtomicSafetyHandle.Create());
#endif
                encodedData.Resize(output.Length, NativeArrayOptions.UninitializedMemory);
                output.CopyTo(encodedData);
                output.Dispose();
            }
        }
    }
}
