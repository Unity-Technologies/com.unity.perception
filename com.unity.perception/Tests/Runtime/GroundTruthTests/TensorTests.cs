using System;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace GroundTruthTests
{
    [TestFixture]
    public class TensorTests
    {
        [Test]
        public void TestSetWrongType()
        {
            var t = new Tensor(Tensor.ElementType.Float, new[] { 2, 2 });
            const int i = 7;
            Assert.Throws<ArgumentException>(() => t.SetElementAt(new[] { 0, 1 }, i));
            const float f = 7;
            Assert.DoesNotThrow(() => t.SetElementAt(new[] {0, 1}, f));
        }

        [Test]
        public void TestSetBadLocations()
        {
            var t = new Tensor(Tensor.ElementType.Float, new[] { 2, 2 });
            const float i = 7;
            Assert.Throws<ArgumentException>(() => t.SetElementAt(new[] { 0 }, i));
            Assert.Throws<ArgumentException>(() => t.SetElementAt(new[] { 1, 8, 14, 27 }, i));
            Assert.Throws<ArgumentException>(() => t.SetElementAt(new[] { 2, 2 }, i));
            Assert.Throws<ArgumentException>(() => t.SetElementAt(new[] { 2, -1 }, i));
            Assert.Throws<ArgumentException>(() => t.SetElementAt(new[] { -1, 0 }, i));
            Assert.Throws<ArgumentException>(() => t.SetElementAt(new[] { 0, 2 }, i));
            Assert.Throws<ArgumentException>(() => t.SetElementAt(new[] { 2, 0 }, i));

            const float f = 7;
            Assert.DoesNotThrow(() => t.SetElementAt(new[] {0, 0}, f));
            Assert.DoesNotThrow(() => t.SetElementAt(new[] {0, 1}, f));
            Assert.DoesNotThrow(() => t.SetElementAt(new[] {1, 0}, f));
            Assert.DoesNotThrow(() => t.SetElementAt(new[] {1, 1}, f));
        }

        [Test]
        public void TestByteTensor()
        {
            var buffer = new byte[27];
            for (var i = 0; i < 27; i++)
            {
                buffer[i] = (byte)i;
            }

            var tensor = new Tensor(Tensor.ElementType.Byte, new[] { 3, 3, 3 }, buffer);

            var idx = new[] { 0, 0, 0 };
            tensor.GetElementAt(idx, out byte b);

            // Test some locations
            Assert.AreEqual(0, b);

            idx[0] = 2;
            idx[1] = 2;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(26, b);

            idx[0] = 1;
            idx[1] = 0;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(11, b);

            const byte setIt = 232;
            tensor.SetElementAt(idx, setIt);
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(setIt, b);
        }

        [Test]
        public void TestFloatTensor()
        {
            var buffer = new byte[27 * sizeof(float)];
            for (var i = 0; i < 27; i++)
            {
                var tmp = BitConverter.GetBytes((float)i);
                Buffer.BlockCopy(tmp, 0, buffer, i * sizeof(float), sizeof(float));
            }

            var tensor = new Tensor(Tensor.ElementType.Float, new[] { 3, 3, 3 }, buffer);

            var idx = new[] { 0, 0, 0 };
            tensor.GetElementAt(idx, out float b);

            // Test some locations
            Assert.AreEqual(0, b);

            idx[0] = 2;
            idx[1] = 2;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(26, b);

            idx[0] = 1;
            idx[1] = 0;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(11, b);

            const float setIt = -312.74f;
            tensor.SetElementAt(idx, setIt);
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(setIt, b);
        }

        [Test]
        public void TestIntTensor()
        {
            var buffer = new byte[27 * sizeof(int)];
            for (var i = 0; i < 27; i++)
            {
                var tmp = BitConverter.GetBytes(i);
                Buffer.BlockCopy(tmp, 0, buffer, i * sizeof(int), sizeof(int));
            }

            var tensor = new Tensor(Tensor.ElementType.Int, new[] { 3, 3, 3 }, buffer);

            var idx = new[] { 0, 0, 0 };
            tensor.GetElementAt(idx, out int b);

            // Test some locations
            Assert.AreEqual(0, b);

            idx[0] = 2;
            idx[1] = 2;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(26, b);

            idx[0] = 1;
            idx[1] = 0;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(11, b);

            const int setIt = 42;
            tensor.SetElementAt(idx, setIt);
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(setIt, b);
        }

        [Test]
        public void TestUintTensor()
        {
            var buffer = new byte[27 * sizeof(uint)];
            for (var i = 0; i < 27; i++)
            {
                var tmp = BitConverter.GetBytes((uint)i);
                Buffer.BlockCopy(tmp, 0, buffer, i * sizeof(uint), sizeof(uint));
            }

            var tensor = new Tensor(Tensor.ElementType.Uint, new[] { 3, 3, 3 }, buffer);

            var idx = new[] { 0, 0, 0 };
            tensor.GetElementAt(idx, out uint b);

            // Test some locations
            Assert.AreEqual(0, b);

            idx[0] = 2;
            idx[1] = 2;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(26, b);

            idx[0] = 1;
            idx[1] = 0;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(11, b);

            const uint setIt = uint.MaxValue - 1;
            tensor.SetElementAt(idx, setIt);
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(setIt, b);
        }

        [Test]
        public void TestDoubleTensor()
        {
            var buffer = new byte[27 * sizeof(double)];
            for (var i = 0; i < 27; i++)
            {
                var tmp = BitConverter.GetBytes((double)i);
                Buffer.BlockCopy(tmp, 0, buffer, i * sizeof(double), sizeof(double));
            }

            var tensor = new Tensor(Tensor.ElementType.Double, new[] { 3, 3, 3 }, buffer);

            var idx = new[] { 0, 0, 0 };
            tensor.GetElementAt(idx, out double b);

            // Test some locations
            Assert.AreEqual(0, b);

            idx[0] = 2;
            idx[1] = 2;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(26, b);

            idx[0] = 1;
            idx[1] = 0;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(11, b);

            const double setIt = 7623.334;
            tensor.SetElementAt(idx, setIt);
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(setIt, b);
        }

        [Test]
        public void TestLongTensor()
        {
            var buffer = new byte[27 * sizeof(long)];
            for (var i = 0; i < 27; i++)
            {
                var tmp = BitConverter.GetBytes((long)i);
                Buffer.BlockCopy(tmp, 0, buffer, i * sizeof(long), sizeof(long));
            }

            var tensor = new Tensor(Tensor.ElementType.Long, new[] { 3, 3, 3 }, buffer);

            var idx = new[] { 0, 0, 0 };
            tensor.GetElementAt(idx, out long b);

            // Test some locations
            Assert.AreEqual(0, b);

            idx[0] = 2;
            idx[1] = 2;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(26, b);

            idx[0] = 1;
            idx[1] = 0;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(11, b);

            const long setIt = long.MaxValue - 87;
            tensor.SetElementAt(idx, setIt);
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(setIt, b);
        }

        [Test]
        public void TestCharTensor()
        {
            var buffer = new byte[27 * sizeof(char)];
            for (var i = 0; i < 27; i++)
            {
                var tmp = BitConverter.GetBytes((char)i);
                Buffer.BlockCopy(tmp, 0, buffer, i * sizeof(char), sizeof(char));
            }

            var tensor = new Tensor(Tensor.ElementType.Char, new[] { 3, 3, 3 }, buffer);

            var idx = new[] { 0, 0, 0 };
            tensor.GetElementAt(idx, out char b);

            // Test some locations
            Assert.AreEqual(0, b);

            idx[0] = 2;
            idx[1] = 2;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(26, b);

            idx[0] = 1;
            idx[1] = 0;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(11, b);

            const char setIt = 'F';
            tensor.SetElementAt(idx, setIt);
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(setIt, b);
        }

        [Test]
        public void TestBoolTensor()
        {
            var buffer = new byte[27 * sizeof(bool)];
            for (var i = 0; i < 27; i++)
            {
                var tmp = BitConverter.GetBytes(i % 2 == 0);
                Buffer.BlockCopy(tmp, 0, buffer, i * sizeof(bool), sizeof(bool));
            }

            var tensor = new Tensor(Tensor.ElementType.Bool, new[] { 3, 3, 3 }, buffer);

            var idx = new[] { 0, 0, 0 };
            tensor.GetElementAt(idx, out bool b);

            // Test some locations
            Assert.AreEqual(true, b);

            idx[0] = 2;
            idx[1] = 2;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(true, b);

            idx[0] = 1;
            idx[1] = 0;
            idx[2] = 2;
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(false, b);

            const bool setIt = true;
            tensor.SetElementAt(idx, setIt);
            tensor.GetElementAt(idx, out b);
            Assert.AreEqual(setIt, b);
        }

        [Test]
        [TestCase(0, 0, 0, 0)]
        [TestCase(255, 255, 255, 255)]
        [TestCase(255, 0, 0, 255)]
        [TestCase(128, 128, 128, 128)]
        public void Color32TensorTest(byte r, byte g, byte b, byte a)
        {
            var c = new Color32(r, g, b, a);
            var t = TensorBuilder.ToTensor(c);
            Assert.AreEqual(1, t.shape.Length);
            Assert.AreEqual(4, t.shape[0]);
            Assert.AreEqual(Tensor.ElementType.Byte, t.elementType);
            Assert.AreEqual(r, t.buffer[0]);
            Assert.AreEqual(g, t.buffer[1]);
            Assert.AreEqual(b, t.buffer[2]);
            Assert.AreEqual(a, t.buffer[3]);
            var c2 = TensorBuilder.ToColor32(t);
            Assert.AreEqual(c, c2);
        }

        [Test]
        [TestCase(0, 0, 0)]
        [TestCase(255, 255, 255)]
        [TestCase(255, 0, 0)]
        [TestCase(128, 128, 128)]
        [TestCase(-128, -128, -128)]
        [TestCase(0.1f, 0.2f, -4.2f)]
        public void Vector3TensorTest(float x, float y, float z)
        {
            var c = new Vector3(x, y, z);
            var t = TensorBuilder.ToTensor(c);
            Assert.AreEqual(1, t.shape.Length);
            Assert.AreEqual(3, t.shape[0]);
            Assert.AreEqual(Tensor.ElementType.Float, t.elementType);

            t.GetElementAt(new[] {0}, out float oX);
            t.GetElementAt(new[] {1}, out float oY);
            t.GetElementAt(new[] {2}, out float oZ);
            Assert.AreEqual(x, oX);
            Assert.AreEqual(y, oY);
            Assert.AreEqual(z, oZ);
            var c2 = TensorBuilder.ToVector3(t);
            Assert.AreEqual(c, c2);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(255, 255)]
        [TestCase(255, 0)]
        [TestCase(128, 128)]
        [TestCase(-128, -128)]
        [TestCase(0.1f, 0.2f)]
        public void Vector2TensorTest(float x, float y)
        {
            var c = new Vector2(x, y);
            var t = TensorBuilder.ToTensor(c);
            Assert.AreEqual(1, t.shape.Length);
            Assert.AreEqual(2, t.shape[0]);
            Assert.AreEqual(Tensor.ElementType.Float, t.elementType);

            t.GetElementAt(new[] {0}, out float oX);
            t.GetElementAt(new[] {1}, out float oY);
            Assert.AreEqual(x, oX);
            Assert.AreEqual(y, oY);
            var c2 = TensorBuilder.ToVector2(t);
            Assert.AreEqual(c, c2);
        }

        [Test]
        public void Float3X3TensorTest()
        {
            var c = new float3x3
            {
                c0 = new float3(0, 1, 2),
                c1 = new float3(3, 4, 5),
                c2 = new float3(6, 7, 8)
            };

            var t = TensorBuilder.ToTensor(c);
            Assert.AreEqual(2, t.shape.Length);
            Assert.AreEqual(3, t.shape[0]);
            Assert.AreEqual(3, t.shape[1]);
            Assert.AreEqual(Tensor.ElementType.Float, t.elementType);

            t.GetElementAt(new[] {0, 0}, out float oX);
            t.GetElementAt(new[] {2, 2}, out float oY);
            Assert.AreEqual(0, oX);
            Assert.AreEqual(8, oY);
            var c2 = TensorBuilder.ToFloat3X3(t);
            Assert.AreEqual(c, c2);
        }
    }
}
