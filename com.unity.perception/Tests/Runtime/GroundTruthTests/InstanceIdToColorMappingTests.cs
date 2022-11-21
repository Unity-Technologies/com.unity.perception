using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.Utilities;

namespace GroundTruthTests
{
    [TestFixture]
    public class InstanceIdToColorMappingTests
    {
        [Test]
        public void InitializeMaps_DoesNotThrow()
        {
            Assert.DoesNotThrow(InstanceIdToColorMapping.InitializeMaps);
        }

        [Test]
        public void InstanceIdToColorMappingTests_TestHslColors()
        {
            for (var i = 1u; i <= 1024u; i++)
            {
                Assert.IsTrue(InstanceIdToColorMapping.TryGetColorFromInstanceId(i, out var color), $"Failed TryGetColorFromInstanceId on id {i}");
                Assert.IsTrue(InstanceIdToColorMapping.TryGetInstanceIdFromColor(color, out var id), $"Failed TryGetInstanceIdFromColor on id {i}");
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
        [TestCase(1u, 255, 0, 0, 255)]
        [TestCase(2u, 0, 74, 255, 255)]
        [TestCase(3u, 149, 255, 0, 255)]
        [TestCase(4u, 255, 0, 223, 255)]
        [TestCase(5u, 0, 255, 212, 255)]
        [TestCase(6u, 255, 138, 0, 255)]
        [TestCase(1024u, 30, 0, 11, 255)]
        [TestCase(1025u, 0, 0, 1, 254)]
        [TestCase(1026u, 0, 0, 2, 254)]
        [TestCase(1024u + 256u, 0, 1, 0, 254)]
        [TestCase(1025u + 256u, 0, 1, 1, 254)]
        [TestCase(1024u + 65536u, 1, 0, 0, 254)]
        [TestCase(1024u + 16777216u, 0, 0, 0, 253)]
        [TestCase(1024u + (16777216u * 2), 0, 0, 0, 252)]
        public void InstanceIdToColorMappingTests_TestColorForId(uint id, byte r, byte g, byte b, byte a)
        {
            Assert.IsTrue(InstanceIdToColorMapping.TryGetColorFromInstanceId(id, out var color));
            var expected = new Color32(r, g, b, a);
            Assert.AreEqual(expected, color);

            Assert.IsTrue(InstanceIdToColorMapping.TryGetInstanceIdFromColor(color, out var id2));
            Assert.AreEqual(id, id2);

            color = InstanceIdToColorMapping.GetColorFromInstanceId(id);
            Assert.AreEqual(expected, color);

            id2 = InstanceIdToColorMapping.GetInstanceIdFromColor(color);
            Assert.AreEqual(id, id2);
        }

        [Test]
        public void InstanceIdToColorMappingTests_GetCorrectValuesFor255()
        {
            var expectedColor = new Color32(19, 210, 0, 255);

            Assert.IsTrue(InstanceIdToColorMapping.TryGetColorFromInstanceId(255u, out var color));
            Assert.AreEqual(expectedColor, color);
            Assert.IsTrue(InstanceIdToColorMapping.TryGetInstanceIdFromColor(color, out var id2));
            Assert.AreEqual(255u, id2);

            color = InstanceIdToColorMapping.GetColorFromInstanceId(255u);
            Assert.AreEqual(expectedColor, color);
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
            var c = new Color32(28, 92, 14, 255);
            Assert.Throws<InvalidOperationException>(() => InstanceIdToColorMapping.GetInstanceIdFromColor(c));
        }

        [Test]
        public void InstanceIdToColorMappingTests_TryGetReturnsFalseIdNotMapped()
        {
            var c = new Color32(28, 92, 14, 255);
            Assert.IsFalse(InstanceIdToColorMapping.TryGetInstanceIdFromColor(c, out var id));
        }
    }
}
