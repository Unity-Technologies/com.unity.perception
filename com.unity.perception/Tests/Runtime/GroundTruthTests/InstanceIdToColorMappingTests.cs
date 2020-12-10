using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace GroundTruthTests
{
    [TestFixture]
    public class InstanceIdToColorMappingTests
    {
        [Test]
        public void InstanceIdToColorMappingTests_TestHslColors()
        {
            for (var i = 1u; i <= 64u; i++)
            {
                Assert.IsTrue(InstanceIdToColorMapping.TryGetColorFromInstanceId(i, out var color));
                Assert.IsTrue(InstanceIdToColorMapping.TryGetInstanceIdFromColor(color, out var id));
                Assert.AreEqual(i, id);

                color = InstanceIdToColorMapping.GetColorFromInstanceId(i);
                id = InstanceIdToColorMapping.GetInstanceIdFromColor(color);
                Assert.AreEqual(i, id);
            }
        }

        [Test]
        [TestCase(0, 0, 0, 255, 255u)]
        [TestCase(255, 0, 0, 255, 4278190335u)]
        [TestCase(0, 255, 0, 255, 16711935u)]
        [TestCase(0, 0, 255, 255, 65535u)]
        [TestCase(255, 255, 255, 255, 4294967295u)]
        [TestCase(0, 0, 1, 255, 511u)]
        [TestCase(127, 64, 83, 27, 2134922011u)]
        public void InstanceIdToColorMappingTests_ToAndFromPackedColor(byte r, byte g, byte b, byte a, uint expected)
        {
            var color = new Color32(r, g, b, a);
            var packed = InstanceIdToColorMapping.GetPackedColorFromColor(color);
            Assert.AreEqual(packed, expected);
            var c = InstanceIdToColorMapping.GetColorFromPackedColor(packed);
            Assert.AreEqual(r, c.r);
            Assert.AreEqual(g, c.g);
            Assert.AreEqual(b, c.b);
            Assert.AreEqual(a, c.a);
        }

        [Test]
        [TestCase(1u, 255,0,0,255)]
        [TestCase(2u,0,74,255,255)]
        [TestCase(3u,149,255,0,255)]
        [TestCase(4u,255,0,223,255)]
        [TestCase(5u,0,255,212,255)]
        [TestCase(6u,255,138,0,255)]
        [TestCase(64u,195,0,75,255)]
        [TestCase(65u,0,0,1,254)]
        [TestCase(66u,0,0,2,254)]
        [TestCase(64u + 256u,0,1,0,254)]
        [TestCase(65u + 256u,0,1,1,254)]
        [TestCase(64u + 65536u,1,0,0,254)]
        [TestCase(16777216u,255,255,192,254)]
        [TestCase(64u + 16777216u,0,0,0,253)]
        [TestCase(64u + (16777216u * 2),0,0,0,252)]
        public void InstanceIdToColorMappingTests_TestColorForId(uint id, byte r, byte g, byte b, byte a)
        {
            Assert.IsTrue(InstanceIdToColorMapping.TryGetColorFromInstanceId(id, out var color));
            Assert.AreEqual(color.r, r);
            Assert.AreEqual(color.g, g);
            Assert.AreEqual(color.b, b);
            Assert.AreEqual(color.a, a);

            Assert.IsTrue(InstanceIdToColorMapping.TryGetInstanceIdFromColor(color, out var id2));
            Assert.AreEqual(id, id2);

            color = InstanceIdToColorMapping.GetColorFromInstanceId(id);
            Assert.AreEqual(color.r, r);
            Assert.AreEqual(color.g, g);
            Assert.AreEqual(color.b, b);
            Assert.AreEqual(color.a, a);

            id2 = InstanceIdToColorMapping.GetInstanceIdFromColor(color);
            Assert.AreEqual(id, id2);
        }

        [Test]
        public void InstanceIdToColorMappingTests_GetCorrectValuesFor255()
        {
            var expectedColor = new Color(0, 0, 191, 254);

            Assert.IsTrue(InstanceIdToColorMapping.TryGetColorFromInstanceId(255u, out var color));
            Assert.AreEqual(color.r, expectedColor.r);
            Assert.AreEqual(color.g, expectedColor.g);
            Assert.AreEqual(color.b, expectedColor.b);
            Assert.AreEqual(color.a, expectedColor.a);
            Assert.IsTrue(InstanceIdToColorMapping.TryGetInstanceIdFromColor(color, out var id2));
            Assert.AreEqual(255u, id2);

            color = InstanceIdToColorMapping.GetColorFromInstanceId(255u);
            Assert.AreEqual(color.r, expectedColor.r);
            Assert.AreEqual(color.g, expectedColor.g);
            Assert.AreEqual(color.b, expectedColor.b);
            Assert.AreEqual(color.a, expectedColor.a);
            id2 = InstanceIdToColorMapping.GetInstanceIdFromColor(color);
            Assert.AreEqual(255u, id2);
        }

        [Test]
        [TestCase(0u)]
        public void InstanceIdToColorMappingTests_GetBlackForId(uint id)
        {
            Assert.IsFalse(InstanceIdToColorMapping.TryGetColorFromInstanceId(id, out var color));
            Assert.AreEqual(color.r, 0);
            Assert.AreEqual(color.g, 0);
            Assert.AreEqual(color.b, 0);
            Assert.AreEqual(color.a, 255);
            Assert.IsFalse(InstanceIdToColorMapping.TryGetInstanceIdFromColor(color, out var id2));
            Assert.AreEqual(0, id2);

            color = InstanceIdToColorMapping.GetColorFromInstanceId(id);
            Assert.AreEqual(color.r, 0);
            Assert.AreEqual(color.g, 0);
            Assert.AreEqual(color.b, 0);
            Assert.AreEqual(color.a, 255);
            id2 = InstanceIdToColorMapping.GetInstanceIdFromColor(color);
            Assert.AreEqual(0, id2);
        }

        [Test]
        public void InstanceIdToColorMappingTests_ThrowExceptionIdTooLarge()
        {
            Assert.Throws<IndexOutOfRangeException>(() => InstanceIdToColorMapping.GetColorFromInstanceId(uint.MaxValue));
            var c = new Color32(255, 255, 255, 0);
            Assert.Throws<IndexOutOfRangeException>(() => InstanceIdToColorMapping.GetInstanceIdFromColor(c));
        }

        [Test]
        public void InstanceIdToColorMappingTests_TryGetReturnsFalseIdTooLarge()
        {
            Assert.IsFalse(InstanceIdToColorMapping.TryGetColorFromInstanceId(uint.MaxValue, out var color));
            color = new Color32(255, 255, 255, 0);
            Assert.IsFalse(InstanceIdToColorMapping.TryGetInstanceIdFromColor(color, out var id));
        }

        [Test]
        public void InstanceIdToColorMappingTests_ThrowExceptionIdNotMapped()
        {
            var c = new Color32(28,92,14,255);
            Assert.Throws<InvalidOperationException>(() => InstanceIdToColorMapping.GetInstanceIdFromColor(c));
        }

        [Test]
        public void InstanceIdToColorMappingTests_TryGetReturnsFalseIdNotMapped()
        {
            var c = new Color32(28,92,14,255);
            Assert.IsFalse(InstanceIdToColorMapping.TryGetInstanceIdFromColor(c, out var id));
        }
    }
}
