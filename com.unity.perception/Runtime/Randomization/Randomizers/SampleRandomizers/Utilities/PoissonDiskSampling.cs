using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Utility for generating lists of poisson disk sampled points
    /// </summary>
    public static class PoissonDiskSampling
    {
        const int k_DefaultSamplingResolution = 30;

        /// <summary>
        /// Returns a list of poisson disc sampled points for a given area and density
        /// </summary>
        /// <param name="width">Width of the sampling area</param>
        /// <param name="height">Height of the sampling area</param>
        /// <param name="minimumRadius">The minimum distance required between each sampled point</param>
        /// <param name="seed">The random seed used to initialize the algorithm state</param>
        /// <param name="samplingResolution">The number of potential points sampled around every valid point</param>
        /// <returns>The list of generated poisson points</returns>
        public static NativeList<float2> GenerateSamples(
            float width,
            float height,
            float minimumRadius,
            uint seed = 12345,
            int samplingResolution = k_DefaultSamplingResolution)
        {
            if (width < 0)
                throw new ArgumentException($"Width {width} cannot be negative");
            if (height < 0)
                throw new ArgumentException($"Height {height} cannot be negative");
            if (minimumRadius < 0)
                throw new ArgumentException($"MinimumRadius {minimumRadius} cannot be negative");
            if (seed == 0)
                throw new ArgumentException("Random seed cannot be 0");
            if (samplingResolution <= 0)
                throw new ArgumentException($"SamplingAttempts {samplingResolution} cannot be <= 0");

            var samples = new NativeList<float2>(Allocator.TempJob);
            new SampleJob
            {
                width = width,
                height = height,
                minimumRadius = minimumRadius,
                seed = seed,
                samplingResolution = samplingResolution,
                samples = samples
            }.Schedule().Complete();
            return samples;
        }

        [BurstCompile]
        struct SampleJob : IJob
        {
            public float width;
            public float height;
            public float minimumRadius;
            public uint seed;
            public int samplingResolution;
            public NativeList<float2> samples;

            public void Execute()
            {
                var newSamples = Sample(width, height, minimumRadius, seed, samplingResolution, Allocator.Temp);
                samples.AddRange(newSamples);
                newSamples.Dispose();
            }
        }

        // Algorithm sourced from Robert Bridson's paper "Fast Poisson Disk Sampling in Arbitrary Dimensions"
        // https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf
        /// <summary>
        /// Returns a list of poisson disc sampled points for a given area and density
        /// </summary>
        /// <param name="width">Width of the sampling area</param>
        /// <param name="height">Height of the sampling area</param>
        /// <param name="minimumRadius">The minimum distance required between each sampled point</param>
        /// <param name="seed">The random seed used to initialize the algorithm state</param>
        /// <param name="samplingResolution">The number of potential points sampled around every valid point</param>
        /// <param name="allocator">The allocator type of the generated native container</param>
        /// <returns>The list of generated poisson points</returns>
        static NativeList<float2> Sample(
            float width,
            float height,
            float minimumRadius,
            uint seed,
            int samplingResolution,
            Allocator allocator)
        {
            var samples = new NativeList<float2>(allocator);

            // Calculate occupancy grid dimensions
            var random = new Unity.Mathematics.Random(seed);
            var cellSize = minimumRadius / math.sqrt(2);
            var rows = Mathf.FloorToInt(height / cellSize);
            var cols = Mathf.FloorToInt(width / cellSize);
            var gridSize = rows * cols;
            if (gridSize == 0)
                return samples;

            // Initialize a few constants
            var rSqr = minimumRadius * minimumRadius;
            var samplingArc = math.PI * 2 / samplingResolution;
            var halfSamplingArc = samplingArc / 2;

            // Initialize a hash array that maps a sample's grid position to it's index
            var gridToSampleIndex = new NativeArray<int>(gridSize, Allocator.Temp);
            for (var i = 0; i < gridSize; i++)
                gridToSampleIndex[i] = -1;

            // This list will track all points that may still have space around them for generating new points
            var activePoints = new NativeList<float2>(Allocator.Temp);

            // Randomly place a seed point in a central location within the generation space to kick off the algorithm
            var firstPoint = new float2(
                random.NextFloat(0.4f, 0.6f) * width,
                random.NextFloat(0.4f, 0.6f) * height);
            samples.Add(firstPoint);
            var firstPointCol = Mathf.FloorToInt(firstPoint.x / cellSize);
            var firstPointRow = Mathf.FloorToInt(firstPoint.y / cellSize);
            gridToSampleIndex[firstPointCol + firstPointRow * cols] = 0;
            activePoints.Add(firstPoint);

            while (activePoints.Length > 0)
            {
                var randomIndex = random.NextInt(0, activePoints.Length);
                var activePoint = activePoints[randomIndex];

                var nextPointFound = false;
                for (var i = 0; i < samplingResolution; i++)
                {
                    var length = random.NextFloat(minimumRadius, minimumRadius * 2);
                    var angle = samplingArc * i + random.NextFloat(-halfSamplingArc, halfSamplingArc);

                    // Generate a new point within the circular placement region around the active point
                    var newPoint = activePoint + new float2(
                        math.cos(angle) * length,
                        math.sin(angle) * length);

                    var col = Mathf.FloorToInt(newPoint.x / cellSize);
                    var row = Mathf.FloorToInt(newPoint.y / cellSize);

                    if (row < 0 || row >= rows || col < 0 || col >= cols)
                        continue;

                    // Iterate over the 8 surrounding grid locations to check if the newly generated point is too close
                    // to an existing point
                    var tooCloseToAnotherPoint = false;
                    for (var x = -2; x <= 2; x++)
                    {
                        if ((col + x) < 0 || (col + x) >= cols)
                            continue;

                        for (var y = -2; y <= 2; y++)
                        {
                            if ((row + y) < 0 || (row + y) >= rows)
                                continue;

                            var gridIndex = (col + x) + (row + y) * cols;
                            if (gridToSampleIndex[gridIndex] < 0)
                                continue;

                            var distanceSqr = math.distancesq(samples[gridToSampleIndex[gridIndex]], newPoint);

                            if (distanceSqr >= rSqr)
                                continue;
                            tooCloseToAnotherPoint = true;
                            break;
                        }
                    }

                    if (tooCloseToAnotherPoint)
                        continue;

                    // If the new point is accepted, add it to the occupancy grid and the list of generated samples
                    nextPointFound = true;
                    activePoints.Add(newPoint);
                    samples.Add(newPoint);
                    gridToSampleIndex[col + row * cols] = samples.Length - 1;
                }

                if (!nextPointFound)
                    activePoints.RemoveAtSwapBack(randomIndex);
            }
            gridToSampleIndex.Dispose();
            activePoints.Dispose();

            return samples;
        }
    }
}
