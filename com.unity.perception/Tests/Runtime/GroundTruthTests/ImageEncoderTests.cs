using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class ImageEncoderTests
    {
        [TearDown]
        public void Teardown()
        {
            ImageEncoder.encodeImagesAsynchronously = true;
            ImageEncoder.WaitForAllEncodingJobsToComplete();
        }

        [UnityTest]
        public IEnumerator EncodingValidationTest(
            [Values(ImageEncodingFormat.Png, ImageEncodingFormat.Jpg, ImageEncodingFormat.Exr, ImageEncodingFormat.Raw)]
            ImageEncodingFormat encodingFormat,
            [Values(false, true)] bool encodeOnJobSystem)
        {
            const int width = 32;

            // Create an all black source texture
            var texture = TestHelper.CreateBlankTexture(width, width, GraphicsFormat.R8G8B8A8_UNorm, Color.black);

            // Draw a few pixels in the source texture so the encoded image can be validated later
            var validationPixels = new List<(int, Color)>();
            validationPixels.Add((0, Color.red));
            validationPixels.Add((width / 2, Color.green));
            validationPixels.Add((width - 1, Color.blue));
            foreach (var pixel in validationPixels)
                texture.SetPixel(pixel.Item1, pixel.Item1, pixel.Item2);
            texture.Apply();

            // Get the raw texture data
            var rawPixelBuffer = texture.GetRawTextureData<Color32>();

            var doneEncoding = false;
            ImageEncoder.encodeImagesAsynchronously = encodeOnJobSystem;
            ImageEncoder.EncodeImage(rawPixelBuffer, width, width, texture.graphicsFormat, encodingFormat, encodedData =>
            {
                doneEncoding = true;

                // Validate that some encoded image bytes were actually generated.
                Assert.Greater(encodedData.Length, 0);

                // Unity doesn't have an API yet for loading EXR files at runtime,
                // so the remaining validation steps are skipped for EXR files.
                if (encodingFormat == ImageEncodingFormat.Exr)
                    return;

                // Reload the encoded data back into a texture for validation
                var validationTexture = new Texture2D(width, width, texture.graphicsFormat, TextureCreationFlags.None);
                validationTexture.filterMode = FilterMode.Point;
                if (encodingFormat == ImageEncodingFormat.Raw)
                    validationTexture.LoadRawTextureData(encodedData.ToArray());
                else
                    Assert.True(ImageConversion.LoadImage(validationTexture, encodedData.ToArray()));
                validationTexture.Apply();

                // Ensure that each validation pixel is present in the encoded image data
                foreach (var pixel in validationPixels)
                {
                    var foundPixel = validationTexture.GetPixel(pixel.Item1, pixel.Item1);
                    if (encodingFormat == ImageEncodingFormat.Jpg)
                        foundPixel = RoundJpegColorToNearestRgb(foundPixel);
                    Assert.AreEqual(pixel.Item2, foundPixel);
                }

                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(validationTexture);
            });

            rawPixelBuffer.Dispose();

            while (!doneEncoding)
            {
                ImageEncoder.ExecutePendingCallbacks();
                yield return null;
            }
        }

        [Test]
        public void ExrEncodingOutputsCorrectChannelBitDepth()
        {
            const int width = 16;
            ImageEncoder.encodeImagesAsynchronously = false;

            var tex16Bit = TestHelper.CreateBlankTexture(width, width, GraphicsFormat.R16G16B16A16_UNorm, Color.black);
            var tex32Bit = TestHelper.CreateBlankTexture(width, width, GraphicsFormat.R32G32B32A32_SFloat, Color.black);

            DrawRectangleInCenterOfTexture(tex16Bit, tex16Bit.width / 2, tex16Bit.height / 2, Color.blue);
            DrawRectangleInCenterOfTexture(tex32Bit, tex32Bit.width / 2, tex32Bit.height / 2, Color.blue);

            var rawData16Bit = tex16Bit.GetRawTextureData<byte>();
            var rawData32Bit = tex32Bit.GetRawTextureData<byte>();

            // Confirm that the 32-bit buffer is twice as large as the 16-bit buffer
            Assert.AreEqual(rawData32Bit.Length, rawData16Bit.Length * 2);

            // Encode 16 bit image
            var size16Bit = 0;
            ImageEncoder.EncodeImage(rawData16Bit, tex16Bit.width, tex16Bit.height, tex16Bit.graphicsFormat,
                ImageEncodingFormat.Exr, encodedData =>
                {
                    size16Bit = encodedData.Length;
                });

            // Encode 32 bit image
            var size32Bit = 0;
            ImageEncoder.EncodeImage(rawData32Bit, tex32Bit.width, tex32Bit.height, tex32Bit.graphicsFormat,
                ImageEncodingFormat.Exr, encodedData =>
                {
                    size32Bit = encodedData.Length;
                });

            // Confirm that the 32-bit EXR file is larger than the 16-bit EXR file
            Assert.Greater(size32Bit, size16Bit);

            rawData16Bit.Dispose();
            rawData32Bit.Dispose();

            Object.DestroyImmediate(tex16Bit);
            Object.DestroyImmediate(tex32Bit);
        }

        static void DrawRectangleInCenterOfTexture(Texture2D texture, int rectWidth, int rectHeight, Color color)
        {
            var xStartCoord = texture.width / 2 - rectWidth / 2;
            var xEndCoord = texture.width / 2 + rectWidth / 2;

            var yStartCoord = texture.height / 2 - rectHeight / 2;
            var yEndCoord = texture.height / 2 + rectHeight / 2;

            for (var i = xStartCoord; i < xEndCoord; i++)
                for (var j = yStartCoord; j < yEndCoord; j++)
                    texture.SetPixel(i, j, color);
            texture.Apply();
        }

        static Color32 RoundJpegColorToNearestRgb(Color color)
        {
            if (color.r > color.g && color.r > color.b)
                return Color.red;
            if (color.g > color.r && color.g > color.b)
                return Color.green;
            if (color.b > color.r && color.b > color.r)
                return Color.blue;
            return Color.black;
        }
    }
}
