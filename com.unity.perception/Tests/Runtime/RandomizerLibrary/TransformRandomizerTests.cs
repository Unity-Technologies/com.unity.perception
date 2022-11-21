using System;
using System.Collections;
using NUnit.Framework;
using RandomizerTests.Internal;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using PerceptionParameters = UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.TestTools;

namespace RandomizerTests
{
    [TestFixture]
    public class TransformRandomizerTests
    {
        FixedLengthScenario m_Scenario;
        TransformRandomizer m_Randomizer;
        TransformRandomizerTag m_Tag;

        Vector3 tagPosition => m_Tag.transform.position;
        Quaternion tagRotation => m_Tag.transform.rotation;
        Vector3 tagScale => m_Tag.transform.localScale;

        [SetUp]
        public void Setup()
        {
            TestUtils.SetupRandomizerTestScene<TransformRandomizer, TransformRandomizerTag>(
                ref m_Scenario, ref m_Randomizer, ref m_Tag,
                ((scenario, randomizer, tag) => {})
            );
        }

        [TearDown]
        public void Teardown()
        {
            TestUtils.ResetScene();
        }

        [UnityTest]
        public IEnumerator OffSetting_DoesNotRandomize()
        {
            m_Tag.shouldRandomizePosition = false;
            m_Tag.shouldRandomizeRotation = false;
            m_Tag.shouldRandomizeScale = false;

            var startingPosition = tagPosition;
            var startingRotation = tagRotation;
            var startingScale = tagScale;

            yield return null;
            yield return null;

            Assert.AreEqual(startingPosition, tagPosition);
            Assert.AreEqual(startingRotation, tagRotation);
            Assert.AreEqual(startingScale, tagScale);
        }

        [UnityTest]
        public IEnumerator AbsoluteMode_ProperlyRandomizesTRS()
        {
            // Position
            m_Tag.shouldRandomizePosition = true;
            m_Tag.positionMode = TransformMethod.Absolute;
            var minPosition = new Vector3(-10f, -10f, -10f);
            var maxPosition = new Vector3(10f, 10f, 10f);
            m_Tag.position = new PerceptionParameters.Vector3Parameter()
            {
                x = new UniformSampler(minPosition.x, maxPosition.x),
                y = new UniformSampler(minPosition.y, maxPosition.y),
                z = new UniformSampler(minPosition.z, maxPosition.z),
            };
            m_Tag.transform.position = new Vector3(1000f, 1000f, 1000f);

            // Rotation
            m_Tag.shouldRandomizeRotation = true;
            m_Tag.rotationMode = TransformMethod.Absolute;
            // We don't use -10 to 10 like for the others since it can loop back to -350 and fail our tests.
            var minRotation = new Vector3(0f, 0, 0f);
            var maxRotation = new Vector3(20f, 20f, 20f);
            m_Tag.rotation = new PerceptionParameters.Vector3Parameter()
            {
                x = new UniformSampler(minRotation.x, maxRotation.x),
                y = new UniformSampler(minRotation.y, maxRotation.y),
                z = new UniformSampler(minRotation.z, maxRotation.z),
            };
            m_Tag.transform.rotation = Quaternion.Euler(100, 100, 100);

            // Scale
            m_Tag.shouldRandomizeScale = true;
            m_Tag.scaleMode = TransformMethod.Absolute;
            m_Tag.useUniformScale = false;
            var minScale = new Vector3(-10f, -10f, -10f);
            var maxScale = new Vector3(10f, 10f, 10f);
            m_Tag.scale = new PerceptionParameters.Vector3Parameter()
            {
                x = new UniformSampler(minScale.x, maxScale.x),
                y = new UniformSampler(minScale.y, maxScale.y),
                z = new UniformSampler(minScale.z, maxScale.z),
            };
            m_Tag.transform.localScale = new Vector3(100, 100, 100);

            Assert.IsTrue(m_Tag.positionMode == TransformMethod.Absolute);
            Assert.IsTrue(m_Tag.rotationMode == TransformMethod.Absolute);
            Assert.IsTrue(m_Tag.scaleMode == TransformMethod.Absolute);

            yield return null;
            yield return null;

            AssetIsBetween(tagPosition, minPosition, maxPosition, "Position");
            AssetIsBetween(tagRotation.eulerAngles, minRotation, maxRotation, "Rotation");
            AssetIsBetween(tagScale, minScale, maxScale, "Scale");
        }

        [UnityTest]
        public IEnumerator RelativeMode_ProperlyRandomizesTRS()
        {
            // Position
            m_Tag.shouldRandomizePosition = true;
            m_Tag.positionMode = TransformMethod.Relative;
            var startingPosition = new Vector3(1000f, 1000f, 1000f);
            var minPosition = new Vector3(-10f, -10f, -10f);
            var maxPosition = new Vector3(10f, 10f, 10f);
            m_Tag.position = new PerceptionParameters.Vector3Parameter()
            {
                x = new UniformSampler(minPosition.x, maxPosition.x),
                y = new UniformSampler(minPosition.y, maxPosition.y),
                z = new UniformSampler(minPosition.z, maxPosition.z),
            };
            m_Tag.transform.position = startingPosition;

            // Rotation
            m_Tag.shouldRandomizeRotation = true;
            m_Tag.rotationMode = TransformMethod.Relative;
            var startingRotation = new Vector3(50, 50, 50);
            // We don't use -10 to 10 like for the others since it can loop back to -350 and fail our tests.
            var minRotation = new Vector3(0f, 0, 0f);
            var maxRotation = new Vector3(20f, 20f, 20f);
            m_Tag.rotation = new PerceptionParameters.Vector3Parameter()
            {
                x = new UniformSampler(minRotation.x, maxRotation.x),
                y = new UniformSampler(minRotation.y, maxRotation.y),
                z = new UniformSampler(minRotation.z, maxRotation.z),
            };
            m_Tag.transform.rotation = Quaternion.Euler(startingRotation);

            // Scale
            m_Tag.shouldRandomizeScale = true;
            m_Tag.scaleMode = TransformMethod.Relative;
            m_Tag.useUniformScale = false;
            var startingScale = new Vector3(100, 100, 100);
            var minScale = new Vector3(-10f, -10f, -10f);
            var maxScale = new Vector3(10f, 10f, 10f);
            m_Tag.scale = new PerceptionParameters.Vector3Parameter()
            {
                x = new UniformSampler(minScale.x, maxScale.x),
                y = new UniformSampler(minScale.y, maxScale.y),
                z = new UniformSampler(minScale.z, maxScale.z),
            };
            m_Tag.transform.localScale = startingScale;

            Assert.IsTrue(m_Tag.positionMode == TransformMethod.Relative);
            Assert.IsTrue(m_Tag.rotationMode == TransformMethod.Relative);
            Assert.IsTrue(m_Tag.scaleMode == TransformMethod.Relative);

            yield return null;
            yield return null;

            AssetIsBetween(tagPosition, startingPosition + minPosition, startingPosition + maxPosition, "Position");
            AssetIsBetween(tagRotation.eulerAngles, startingRotation + minRotation, startingRotation + maxRotation, "Rotation");
            AssetIsBetween(
                tagScale,
                new Vector3(startingScale.x * minScale.x, startingScale.y * minScale.y, startingScale.z * minScale.z),
                new Vector3(startingScale.x * maxScale.x, startingScale.y * maxScale.y, startingScale.z * maxScale.z),
                "Scale"
            );
        }

        static void AssetIsBetween(Vector3 val, Vector3 min, Vector3 max, string description = "")
        {
            Assert.IsTrue(val.x <= max.x && val.x >= min.x, $"{description} X-axis value not in specified range: {min.x} <= {val.x} <= {max.x}");
            Assert.IsTrue(val.y <= max.y && val.y >= min.y, $"{description} Y-axis value not in specified range: {min.y} <= {val.y} <= {max.y}");
            Assert.IsTrue(val.z <= max.z && val.z >= min.z, $"{description} Z-axis value not in specified range: {min.z} <= {val.z} <= {max.z}");
        }
    }
}
