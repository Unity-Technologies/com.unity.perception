using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A utility for generating textures of float values where each value represents the total observable surface area
    /// captured by a pixel at that coordinate. Pixel counts of objects in an image can be distorted by a
    /// camera's perspective projection, so taking a weighted count of the visible surface area an object occupies in
    /// the camera frame can enable the following tasks:
    /// <list type="number">
    /// <item>Determining the exact percentage of an object's visible unoccluded surface area, regardless of it's position within the frame.</item>
    /// <item>Determining the observable surface area occupied by an object across multiple camera captures (especially useful for cubemaps).</item>
    /// </list>
    /// </summary>
    /// <remark>
    /// Since the total observable surface area around a camera is equivalent
    /// to the surface area of a unit sphere (4 * pi), the total sum of all of the values in a generated 90 degree fov
    /// square pixel weights texture would be equal to a sixth of the surface area of a sphere. This is because a
    /// square 90 degree fov texture would capture the same area as one face of a cubemap, and it takes 6 cubemap faces
    /// to capture an entire scene.
    /// </remark>
    static class PixelWeightsUtility
    {
        static readonly ProfilerMarker k_CountPixelsInThreads = new ProfilerMarker("Count pixels in threads");
        static readonly ProfilerMarker k_ReducePixelCounts = new ProfilerMarker("ReducePixelCounts");

        static readonly int k_PropPixelWeights = Shader.PropertyToID("pixelWeights");
        static readonly int k_PropWidth = Shader.PropertyToID("width");
        static readonly int k_PropHeight = Shader.PropertyToID("height");
        static readonly int k_PropHFov = Shader.PropertyToID("horizontalFov");
        static readonly int k_PropVFov = Shader.PropertyToID("verticalFov");

        static ComputeShader s_Shader;
        static int3 s_ThreadGroupSize;

        static PixelWeightsUtility()
        {
            s_Shader = ComputeUtilities.LoadShader("CalculatePixelWeights");
            s_ThreadGroupSize = ComputeUtilities.GetKernelThreadGroupSizes(s_Shader, 0);
        }

        /// <summary>
        /// Uses a compute shader to assign a weight to each pixel in a texture based on how much observable space in a
        /// perspective projection is captured within the bounds of that pixel.
        /// See <see cref="CalculatePixelWeightsForSegmentationImage"/> for more details
        /// on how these weights are calculated.
        /// </summary>
        /// <param name="width">The width of the pixel weights texture to generate.</param>
        /// <param name="height">The width of the pixel weights texture to generate.</param>
        /// <param name="verticalFov">The vertical field of view of the perspective projection.</param>
        /// <param name="aspect">The aspect ratio of the texture to generate.</param>
        /// <returns>The generated pixel weights texture.</returns>
        public static RenderTexture GeneratePixelWeights(
            int width, int height, float verticalFov = 90f, float aspect = 1f)
        {
            var vFovRads = verticalFov * Mathf.Deg2Rad;
            var hFovRads = Camera.VerticalToHorizontalFieldOfView(verticalFov, aspect) * Mathf.Deg2Rad;

            var pixelWeights = ComputeUtilities.CreateFloatTexture(width, height);
            s_Shader.SetTexture(0, k_PropPixelWeights, pixelWeights);
            s_Shader.SetInt(k_PropWidth, width);
            s_Shader.SetInt(k_PropHeight, height);
            s_Shader.SetFloat(k_PropVFov, vFovRads);
            s_Shader.SetFloat(k_PropHFov, hFovRads);

            var threadGroupsX = ComputeUtilities.ThreadGroupsCount(width, s_ThreadGroupSize.x);
            var threadGroupsY = ComputeUtilities.ThreadGroupsCount(height, s_ThreadGroupSize.y);
            s_Shader.Dispatch(
                0, threadGroupsX, threadGroupsY, 1);

            return pixelWeights;
        }

        /// <summary>
        /// Assign a weight to each pixel in a segmentation image based on how much observable space is
        /// captured within their bounds assuming the segmentation image is captured with a perspective projection.
        /// </summary>
        /// <note>
        /// For perspective projections, pixels nearest to the center of the camera's
        /// field of view capture a larger portion of observable space than pixels located further toward the edges.
        /// Removing perspective projection bias is key to preventing the location of objects within an image from
        /// distorting their visibility metrics.
        /// The document linked below provides a more in-depth explanation of
        /// perspective projection bias and how to correct for it:
        /// https://docs.google.com/document/d/18Or_zxnb9g0dAkf-vWBdfG4MfW73MBJlpN8ETY3DkmU/edit?usp=sharing
        /// </note>
        /// <param name="width">The width of the segmentation image.</param>
        /// <param name="height">The height of the segmentation image.</param>
        /// <param name="fieldOfView">The vertical field of view of the perspective projection.</param>
        /// <returns>The generated pixel weights.</returns>
        public static NativeArray<float> CalculatePixelWeightsForSegmentationImage(
            int width, int height, float fieldOfView)
        {
            var pixelWeights = new NativeArray<float>(width * height, Allocator.Persistent);

            // Determine the horizontal and vertical field-of-views in radians.
            var aspect = width / (float)height;
            var vFovDeg = fieldOfView;
            var hFovDeg = Camera.VerticalToHorizontalFieldOfView(vFovDeg, aspect);
            var vFovRad = vFovDeg * Mathf.Deg2Rad;
            var hFovRad = hFovDeg * Mathf.Deg2Rad;

            // Calculate the max width and height of the parametric fov surface.
            var xMax = Mathf.Tan(hFovRad / 2f);
            var yMax = Mathf.Tan(vFovRad / 2f);

            // Calculate the width and height of a single parametric pixel.
            var pixelWidth = xMax / (width / 2f);
            var pixelHeight = yMax / (height / 2f);

            for (var pixelX = 0; pixelX < width; pixelX++)
            {
                for (var pixelY = 0; pixelY < height; pixelY++)
                {
                    // Calculate the parametric surface coordinates for the bottom left corner of the current pixel.
                    // See the ObservableSurfaceAreaIntegral notes for more information.
                    var t = xMax * (Mathf.Abs(2f * pixelX - (width - 1)) - 1) / width;
                    var s = yMax * (Mathf.Abs(2f * pixelY - (height - 1)) - 1) / height;

                    // Calculate the parametric spherical surface area integral at each corner of the current pixel.
                    var bottomLeft  = ObservableSurfaceAreaIntegral(t             , s);
                    var topLeft     = ObservableSurfaceAreaIntegral(t             , s + pixelHeight);
                    var bottomRight = ObservableSurfaceAreaIntegral(t + pixelWidth, s);
                    var topRight    = ObservableSurfaceAreaIntegral(t + pixelWidth, s + pixelHeight);

                    // The observable surface area occupied by the current pixel can be calculated by
                    // appropriately combining the overlapping corner integrals.
                    var surfaceArea = topRight - topLeft - bottomRight + bottomLeft;

                    // Store the calculated pixel weight.
                    pixelWeights[pixelY * width + pixelX] = surfaceArea;
                }
            }

            return pixelWeights;
        }

        /// <summary>
        /// Returns the spherical observable surface area contained within a slice of a perspective projection.
        /// Combining four of these integrals, calculated for each corner coordinate of a pixel, can isolate
        /// the observable space contained within a single pixel.
        /// </summary>
        /// <note>
        /// The parameters t and s are parametric coordinates that map a camera's spherical field-of-view to the linear
        /// surface of a perspective projection. The formula below assumes t and s are equivalent to the width and
        /// height of the rectangle that would entirely occupy a quarter of the camera's field-of-view when placed
        /// 1 meter from the camera. Note that t and s will asymptotically increase toward infinity as the camera's
        /// vertical or horizontal field-of-view approaches 180 degrees.
        /// </note>
        /// <param name="t">The horizontal parametric coordinate.</param>
        /// <param name="s">The vertical parametric coordinate.</param>
        /// <returns></returns>
        static float ObservableSurfaceAreaIntegral(float t, float s)
        {
            return Mathf.Atan(t * s / Mathf.Sqrt(1 + t * t + s * s));
        }

        /// <summary>
        /// Count the weighted sum of pixels occupied by each integer id in the given segmentation texture.
        /// </summary>
        /// <param name="idPixels">An array containing the color stored at each pixel of the segmentation image.</param>
        /// <param name="pixelWeights">An array containing relative observable surface area occupied by each pixel in a segmentation image.</param>
        /// <param name="objectCount">The number of objects present in the scene.</param>
        /// <returns></returns>
        public static NativeArray<float> WeightedPixelCountsById(
            NativeArray<uint> idPixels, NativeArray<float> pixelWeights, int objectCount)
        {
            var threadCount = math.max(1, JobsUtility.JobWorkerMaximumCount);
            var pixelCountsRle = new NativeList<RunLengthEncodedIdCount>[threadCount];
            var jobHandle = new JobHandle();

            // Calculate the run-length-encoded (RLE) pixel counts for each unique id
            // present in the segmentation image.
            using (k_CountPixelsInThreads.Auto())
            {
                var sliceLength = idPixels.Length / (double)threadCount;
                for (var i = 0; i < threadCount; i++)
                {
                    var startIndex = (int)(sliceLength * i);
                    var endIndex = (int)(sliceLength * (i + 1));
                    var length = (i == threadCount - 1 ? idPixels.Length : endIndex) - startIndex;

                    // Identify a portion of the segmentation image to work on in a new job thread.
                    var pixelDataSlice = new NativeSlice<uint>(idPixels, startIndex, length);
                    var pixelWeightsSlice = new NativeSlice<float>(pixelWeights, startIndex, length);

                    // Schedule a new job thread to run-length-encode (RLE) a portion of the segmentation image.
                    pixelCountsRle[i] = new NativeList<RunLengthEncodedIdCount>(8, Allocator.TempJob);
                    var countJob = new RunLengthEncodeIdCountsJob
                    {
                        pixelDataSlice = pixelDataSlice,
                        pixelWeightsSlice = pixelWeightsSlice,
                        rleCounts = pixelCountsRle[i]
                    }.Schedule();
                    jobHandle = JobHandle.CombineDependencies(jobHandle, countJob);
                }
                jobHandle.Complete();
            }

            // Merge all the RLE pixel counts calculated in each thread into one hash map array.
            var visiblePixelsPerObject = new NativeArray<float>(objectCount, Allocator.Persistent);
            using (k_ReducePixelCounts.Auto())
            {
                foreach (var counts in pixelCountsRle)
                {
                    for (var i = 0; i < counts.Length; i++)
                    {
                        var rleCount = counts[i];
                        visiblePixelsPerObject[(int)rleCount.id - 1] += rleCount.pixelCount;
                    }
                    counts.Dispose();
                }
            }

            return visiblePixelsPerObject;
        }

        struct RunLengthEncodedIdCount
        {
            public uint id;
            public float pixelCount;
        }

        /// <summary>
        /// Compresses a sequence of integer pixels into a list of id-count pairs.
        /// For example: the pixel sequence [0, 0, 0, 0, 7, 7, 4] will be converted into the
        ///     id-count list [(0, 4), (7, 2), (4, 1)].
        /// </summary>
        [BurstCompile]
        struct RunLengthEncodeIdCountsJob : IJob
        {
            [ReadOnly] public NativeSlice<uint> pixelDataSlice;
            [ReadOnly] public NativeSlice<float> pixelWeightsSlice;
            public NativeList<RunLengthEncodedIdCount> rleCounts;

            public void Execute()
            {
                var currentCount = new RunLengthEncodedIdCount { id = 0 };
                for (var i = 0; i < pixelDataSlice.Length; i++)
                {
                    var pixelWeight = pixelWeightsSlice[i];
                    var id = pixelDataSlice[i];
                    if (id == currentCount.id)
                    {
                        currentCount.pixelCount += pixelWeight;
                    }
                    else
                    {
                        if (currentCount.id != 0)
                            rleCounts.Add(currentCount);

                        currentCount = new RunLengthEncodedIdCount
                        {
                            id = id,
                            pixelCount = pixelWeight
                        };
                    }
                }
                if (currentCount.id != 0)
                    rleCounts.Add(currentCount);
            }
        }
    }
}
