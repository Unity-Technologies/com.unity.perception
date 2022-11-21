using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class ComputeUtilityTests
    {
        static TestCaseData[] PixelWeightsTestCases()
        {
            return new[]
            {
                new TestCaseData(1f, 4f / 3f, 800),
                new TestCaseData(179f, 4f / 3f, 800),
                new TestCaseData(90f, 1f, 256),
                new TestCaseData(90f, 1f, 512),
                new TestCaseData(90f, 1f, 1024),
                new TestCaseData(60f, 1f, 256),
                new TestCaseData(60f, 0.5f, 256),
                new TestCaseData(60f, 2.0f, 256),
                new TestCaseData(60f, 16f / 9f, 256),
                new TestCaseData(30f, 4f / 3f, 800)
            };
        }

        [Test]
        [TestCaseSource(nameof(PixelWeightsTestCases))]
        public void ValidatePixelWeightsNativeArray(float fov, float aspect, int width)
        {
            // Calculate the pixel weights for the given camera specs.
            var height = Mathf.RoundToInt(width / aspect);
            var nativeWeights = PixelWeightsUtility.CalculatePixelWeightsForSegmentationImage(width, height, fov);
            var sum = SumUtility.FloatArraySum(nativeWeights);
            nativeWeights.Dispose();

            // Check if the sum of the piece-wise pixel weights is equal to the observable surface area
            // that would be captured by a camera with the given fov and aspect ratio.
            var expectedSurfaceArea = FovSurfaceArea(fov, aspect);
            Assert.AreEqual(expectedSurfaceArea, sum, 0.01f);
        }

        [Test]
        [TestCaseSource(nameof(PixelWeightsTestCases))]
        public void ValidatePixelWeightsTexture(float fov, float aspect, int width)
        {
            // Calculate the pixel weights for the given camera specs.
            var height = Mathf.RoundToInt(width / aspect);
            var pixelWeightsTexture = PixelWeightsUtility.GeneratePixelWeights(width, height, fov, aspect);

            AsyncGPUReadback.Request(pixelWeightsTexture, 0, request =>
            {
                var weights = request.GetData<float>();
                var sum = SumUtility.FloatArraySum(weights);
                weights.Dispose();

                // Check if the sum of the piece-wise pixel weights is equal to the observable surface area
                // that would be captured by a camera with the given fov and aspect ratio.
                var expectedSurfaceArea = FovSurfaceArea(fov, aspect);
                Assert.AreEqual(expectedSurfaceArea, sum, 0.01f);
            });
            AsyncGPUReadback.WaitAllRequests();
        }

        [Test]
        public void ValidateClearFloatBufferUtility()
        {
            // Create a new ComputeBuffer and fill it with zeros.
            const int bufferSize = 256;
            var floatBuffer = new ComputeBuffer(bufferSize, sizeof(float));
            floatBuffer.SetData(new float[bufferSize]);

            // Set every value of the compute buffer to 1.
            var cmd = CommandBufferPool.Get("Clear Float Buffer Test");
            ClearUtility.ClearFloatBuffer(cmd, floatBuffer, 1.0f);

            cmd.RequestAsyncReadback(floatBuffer, request =>
            {
                // Since the float buffer is filled with ones, the sum of the values
                // in the buffer should be equal to the buffer's pixel count.
                var values = request.GetData<float>();
                var sum = SumUtility.FloatArraySum(values);
                Assert.AreEqual(bufferSize, sum, 0.001f);
            });
            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            floatBuffer.Release();
        }

        [Test]
        public void ValidateClearFloatTextureUtility()
        {
            // Create a square float texture.
            const int sqrWidth = 256;
            const int pixelCount = sqrWidth * sqrWidth;
            var floatTexture = ComputeUtilities.CreateFloatTexture(256, 256);

            // Set the value of every pixel in the texture to 1.
            var cmd = CommandBufferPool.Get("Clear Float Texture Test");
            ClearUtility.ClearFloatTexture(cmd, floatTexture, 1.0f);

            // Since Unity's AsyncReadback API does not currently support the R32_SFloat texture format,
            // we can blit the float texture to a different texture using a supported format (R32G32B32A32_SFloat) and
            // readback the values from this surrogate texture instead.
            var surrogateTexture = new RenderTexture(sqrWidth, sqrWidth, 0, GraphicsFormat.R32G32B32A32_SFloat);
            surrogateTexture.Create();
            cmd.Blit(floatTexture, surrogateTexture);

            cmd.RequestAsyncReadback(surrogateTexture, request =>
            {
                // Sum the values readback from the surrogate texture.
                var values = request.GetData<float4>();
                var sum = 0f;
                for (var i = 0; i < values.Length; i++)
                    sum += values[i].x;
                values.Dispose();

                // Since the float texture is filled with ones, the sum of the values
                // in the texture should be equal to the texture's pixel count.
                Assert.AreEqual(pixelCount, sum, 0.001f);
            });
            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            surrogateTexture.Release();
            floatTexture.Release();
        }

        [Test]
        [TestCaseSource(nameof(PixelWeightsTestCases))]
        [UnityPlatform(exclude = new[] {RuntimePlatform.OSXPlayer})]
        public void ValidateMaskUtility(float fov, float aspect, int width)
        {
            // Create a square RenderTexture that can envelope another RenderTexture
            // with the given width and aspect ratio.
            var height = Mathf.RoundToInt(width / aspect);
            var rtWidth = Mathf.NextPowerOfTwo(Mathf.Max(width, height));

            // Generate pixel weights RenderTexture.
            var pixelWeightsTexture = PixelWeightsUtility.GeneratePixelWeights(width, height, fov, aspect);

            // Create a mask texture where half of the viewport filled with the color white.
            var maskTexture = new RenderTexture(rtWidth, rtWidth, 0, GraphicsFormat.R8G8B8A8_UNorm);
            HalfFillViewPortWithColor(maskTexture, new Color32(255, 255, 255, 255), width, height);

            // Create an output texture.
            var outputTexture = ComputeUtilities.CreateFloatTexture(rtWidth, rtWidth);

            // Execute the TextureMaskingUtility.
            var cmd = CommandBufferPool.Get("TextureMaskingUtility Test");
            MaskUtility.MaskFloatTexture(cmd, pixelWeightsTexture, maskTexture, outputTexture);
            cmd.RequestAsyncReadback(outputTexture, request =>
            {
                // Calculate the sum of all the pixel weights.
                var weights = request.GetData<float>();
                var sum = SumUtility.FloatArraySum(weights);

                // Release allocated resources.
                weights.Dispose();
                pixelWeightsTexture.Release();
                maskTexture.Release();
                outputTexture.Release();

                // Check if the sum of the piece-wise pixel weights is equal to half the observable surface area
                // that would be captured by a camera with the given fov and aspect ratio.
                var expectedSurfaceArea = FovSurfaceArea(fov, aspect) / 2f;
                Assert.AreEqual(expectedSurfaceArea, sum, 0.01f);
            });
            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        [Test]
        public void ValidateSumUtility(
            [Values(256, 512, 1024, 400)] int width)
        {
            // Create a square input float texture.
            var pixelCount = width * width;
            var floatTexture = ComputeUtilities.CreateFloatTexture(width, width);

            // Fill the square float texture with ones.
            var cmd = CommandBufferPool.Get("Float Texture Sum Test");
            ClearUtility.ClearFloatTexture(cmd, floatTexture, 1.0f);

            // Create a new resultBuffer and initialize its values to zero.
            var resultBuffer = new ComputeBuffer(1, sizeof(float));
            resultBuffer.SetData(new[] { 0f });

            // Calculate the sum of all the float values in the texture.
            SumUtility.SumFloatTexture(cmd, floatTexture, resultBuffer, 0);
            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            // Readback the calculated sum.
            var sumResults = new float[1];
            resultBuffer.GetData(sumResults);

            // Release allocated resources.
            floatTexture.Release();
            resultBuffer.Release();

            // Since each pixel weight in the float texture was set to 1, the sum of all the pixel weights in the
            // texture should be equal to the texture's pixel count.
            Assert.AreEqual(pixelCount, sumResults[0], 0.001f);
        }

        [Test]
        public void ValidateFovMaskUtility(
            [Values(60, 90, 120)] float fieldOfView,
            [Values(9f / 16f, 1, 16f / 9f)] float aspect,
            [Values(128, 256)] int width)
        {
            // Create a pixel weights texture and another texture that will contain the masked output.
            var pixelWeightsTexture = PixelWeightsUtility.GeneratePixelWeights(width, width);
            var maskedWeightsTexture = PixelWeightsUtility.GeneratePixelWeights(width, width);

            var pixelWeightsSum = 0f;
            var cmd = CommandBufferPool.Get("FovMaskUtility Test");
            for (var directionIndex = 0; directionIndex < 6; directionIndex++)
            {
                // Mask the pixel weights based on the current directionIndex selected for this test.
                FovMaskUtility.MaskByFov(
                    cmd, pixelWeightsTexture, maskedWeightsTexture, fieldOfView, aspect, directionIndex);
                cmd.RequestAsyncReadback(maskedWeightsTexture, request =>
                {
                    // Calculate the sum of the masked pixels.
                    var maskedPixelWeights = request.GetData<float>();
                    pixelWeightsSum += SumUtility.FloatArraySum(maskedPixelWeights);
                    maskedPixelWeights.Dispose();
                });
            }

            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            pixelWeightsTexture.Release();
            maskedWeightsTexture.Release();

            // Confirm that the masked pixelWeights sum is equal to area of a sphere minus the camera's fov surface area.
            var fovSurfaceArea = FovSurfaceArea(fieldOfView, aspect);
            var surfaceAreaOutsideFov = 4 * Mathf.PI - fovSurfaceArea;
            var onePercentError = 4 * Mathf.PI / 100f;
            Assert.AreEqual(surfaceAreaOutsideFov, pixelWeightsSum, onePercentError);
        }

        /// <summary>
        /// Returns the observable spherical surface area that would be captured within the given
        /// field of view and camera aspect ratio.
        /// </summary>
        /// <param name="verticalFov">The camera's field of view (vertical).</param>
        /// <param name="aspect">The camera's aspect ratio (width / height).</param>
        /// <returns>The captured surface area.</returns>
        static float FovSurfaceArea(float verticalFov, float aspect)
        {
            var vFovRads = verticalFov * Mathf.Deg2Rad;
            var hFovRads = Camera.VerticalToHorizontalFieldOfView(verticalFov, aspect) * Mathf.Deg2Rad;

            var t = Mathf.Tan(vFovRads / 2.0f);
            var s = Mathf.Tan(hFovRads / 2.0f);
            return 4f * Mathf.Atan(t * s / Mathf.Sqrt(1f + t * t + s * s));
        }

        static void HalfFillViewPortWithColor(
            RenderTexture renderTexture, Color32 color, int viewportWidth, int viewportHeight)
        {
            // Create a color array and fill it halfway with the given color.
            var width = viewportWidth;
            var halfHeight = viewportHeight / 2;
            var pixelCount = width * halfHeight;
            var colors = new NativeArray<Color32>(pixelCount, Allocator.Temp);
            for (var i = 0; i < pixelCount; i++)
                colors[i] = color;

            // Write the color array to the GPU.
            var texture = new Texture2D(width, halfHeight, renderTexture.graphicsFormat, TextureCreationFlags.None);
            texture.SetPixelData(colors, 0);
            texture.Apply();

            // Copy the colors into the given RenderTexture.
            var prevActiveRenderTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Graphics.CopyTexture(texture, 0, 0, 0, 0, width, halfHeight, renderTexture, 0, 0, 0, 0);
            RenderTexture.active = prevActiveRenderTexture;

            // Release allocated resources.
            Object.DestroyImmediate(texture);
            colors.Dispose();
        }
    }
}
