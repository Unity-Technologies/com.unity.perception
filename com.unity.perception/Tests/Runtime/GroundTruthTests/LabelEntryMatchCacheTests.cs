﻿using System.Collections;
using NUnit.Framework;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class LabelEntryMatchCacheTests : GroundTruthTestBase
    {
        [Test]
        public void TryGet_ReturnsFalse_ForInvalidInstanceId()
        {
            var config = new IdLabelConfig();
            using (var cache = new LabelEntryMatchCache(config))
            {
                Assert.IsFalse(cache.TryGetLabelEntryFromInstanceId(100, out var labelEntry, out var index));
                Assert.AreEqual(-1, index);
                Assert.AreEqual(default(IdLabelEntry), labelEntry);
            }
        }
        [UnityTest]
        public IEnumerator TryGet_ReturnsTrue_ForMatchingLabel()
        {
            var label = "label";
            var labeledPlane = TestHelper.CreateLabeledPlane(label: label);
            AddTestObjectForCleanup(labeledPlane);
            var config = new IdLabelConfig();
            config.Init(new[]
            {
                new IdLabelEntry()
                {
                    id = 1,
                    label = label
                },
            });
            using (var cache = new LabelEntryMatchCache(config))
            {
                //allow label to be registered
                yield return null;
                Assert.IsTrue(cache.TryGetLabelEntryFromInstanceId(labeledPlane.GetComponent<Labeling>().instanceId, out var labelEntry, out var index));
                Assert.AreEqual(0, index);
                Assert.AreEqual(config.labelEntries[0], labelEntry);
            }
        }
        [UnityTest]
        public IEnumerator TryGet_ReturnsFalse_ForNonMatchingLabel()
        {
            var label = "label";
            var labeledPlane = TestHelper.CreateLabeledPlane(label: label);
            AddTestObjectForCleanup(labeledPlane);
            var config = new IdLabelConfig();
            using (var cache = new LabelEntryMatchCache(config))
            {
                //allow label to be registered
                yield return null;
                Assert.IsFalse(cache.TryGetLabelEntryFromInstanceId(labeledPlane.GetComponent<Labeling>().instanceId, out var labelEntry, out var index));
                Assert.AreEqual(-1, index);
                Assert.AreEqual(default(IdLabelEntry), labelEntry);
            }
        }
        [UnityTest]
        public IEnumerator TryGet_ReturnsFalse_ForNonMatchingLabel_WithOtherMatches()
        {
            var label = "label";
            //only way to guarantee registration order is to run frames.
            //We want to ensure labeledPlane is registered before labeledPlane2 so that the cache does not early out
            var labeledPlane = TestHelper.CreateLabeledPlane(label: "foo");
            AddTestObjectForCleanup(labeledPlane);
            yield return null;
            var labeledPlane2 = TestHelper.CreateLabeledPlane(label: label);
            AddTestObjectForCleanup(labeledPlane2);
            var config = new IdLabelConfig();
            config.Init(new[]
            {
                new IdLabelEntry()
                {
                    id = 1,
                    label = label
                },
            });
            using (var cache = new LabelEntryMatchCache(config))
            {
                //allow label to be registered
                yield return null;
                Assert.IsFalse(cache.TryGetLabelEntryFromInstanceId(labeledPlane.GetComponent<Labeling>().instanceId, out var labelEntry, out var index));
                Assert.AreEqual(-1, index);
                Assert.AreEqual(default(IdLabelEntry), labelEntry);
            }
        }

    }
}
