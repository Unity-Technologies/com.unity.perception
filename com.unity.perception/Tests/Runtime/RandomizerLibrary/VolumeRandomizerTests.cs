#if HDRP_PRESENT
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RandomizerTests.Internal;
using UnityEngine;
using UnityEngine.Perception.Randomization;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Perception.Randomization.VolumeEffects;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.TestTools;
using Vector2Parameter = UnityEngine.Perception.Randomization.Parameters.Vector2Parameter;

namespace RandomizerTests
{
    [TestFixture]
    public class VolumeRandomizerTests
    {
        FixedLengthScenario m_Scenario;
        VolumeRandomizer m_Randomizer;
        VolumeRandomizerTag m_Tag;

        [SetUp]
        public void Setup()
        {
            TestUtils.SetupRandomizerTestScene<VolumeRandomizer, VolumeRandomizerTag>(
                ref m_Scenario, ref m_Randomizer, ref m_Tag,
                ((scenario, randomizer, tag) =>
                {
                    m_Tag.enableEffect = new BooleanParameter() { threshold = 0f };
                })
            );
        }

        [TearDown]
        public void Teardown()
        {
            TestUtils.ResetScene();
        }

        #region Helpers

        /// <summary>
        /// Adds a single volume effect to the <see cref="VolumeRandomizerTag" />
        /// </summary>
        T SetupEffectForTag<T>() where T : VolumeEffect
        {
            var effect = Activator.CreateInstance<T>();
            m_Tag.usedEffects = new List<VolumeEffect>()
            {
                effect
            };
            m_Tag.Setup();

            return effect;
        }

        /// <summary>
        /// Compares whether all properties of a VolumeComponent changed over the course of one frame.
        /// </summary>
        /// <param name="volumeComponent">For example, <see cref="MotionBlur"/> or <see cref="Bloom"/></param>
        /// <param name="vars">A list of functors that access certain properties of the volume component.</param>
        /// <returns></returns>
        static IEnumerator VolumeComponentValuesDidChange<T>(T volumeComponent, Func<T, object[]> vars) where T : VolumeComponent
        {
            var initialValues = vars(volumeComponent);
            yield return null;
            var changedValues = vars(volumeComponent);

            for (var i = 0; i < initialValues.Length; i++)
            {
                Assert.AreNotEqual(initialValues[i], changedValues[i]);
            }
        }

        #endregion

        [UnityTest]
        public IEnumerator Effect_Bloom_RandomizesProperly()
        {
            var effect = SetupEffectForTag<BloomEffect>();
            yield return VolumeComponentValuesDidChange(effect.bloom, vc =>
            {
                return new object[]
                {
                    vc.intensity.value,
                    vc.intensity.value,
                    vc.scatter.value
                };
            });
        }

        [UnityTest]
        public IEnumerator Effect_CameraType_RandomizesProperly()
        {
            var effect = SetupEffectForTag<CameraTypeEffect>();
            var camera = GameObject.FindGameObjectWithTag(TestUtils.cameraTag).GetComponent<Camera>();

            // Spec 1
            var cameraSpec1 = ScriptableObject.CreateInstance<CameraSpecification>();
            cameraSpec1.specificationDescription = "Test spec 1";
            cameraSpec1.focalLength = 2;
            cameraSpec1.lensShift = new Vector2(2, 2);
            cameraSpec1.gateFitMode = Camera.GateFitMode.Fill;
            cameraSpec1.sensorSize = new Vector2(2, 2);

            // Spec 2
            var cameraSpec2 = ScriptableObject.CreateInstance<CameraSpecification>();
            cameraSpec1.specificationDescription = "Test spec 2";
            cameraSpec1.focalLength = 4;
            cameraSpec1.lensShift = new Vector2(4, 4);
            cameraSpec1.gateFitMode = Camera.GateFitMode.Horizontal;
            cameraSpec1.sensorSize = new Vector2(4, 4);

            // Setup camera
            effect.targetCamera = camera;
            effect.cameraSpecifications = new CategoricalParameter<CameraSpecification>();

            // Test spec 1
            effect.cameraSpecifications.SetOptions(new[]
            {
                cameraSpec1
            });

            yield return null;

            // Did the camera get the new spec 1?
            Assert.AreEqual(camera.focalLength, cameraSpec1.focalLength);
            Assert.AreEqual(camera.lensShift, cameraSpec1.lensShift);
            Assert.AreEqual(camera.gateFit, cameraSpec1.gateFitMode);
            Assert.AreEqual(camera.sensorSize, cameraSpec1.sensorSize);

            // Test one more config
            effect.cameraSpecifications.SetOptions(new[]
            {
                cameraSpec2
            });
            yield return null;

            // Did the camera get the new spec 2?
            Assert.AreEqual(camera.focalLength, cameraSpec2.focalLength);
            Assert.AreEqual(camera.lensShift, cameraSpec2.lensShift);
            Assert.AreEqual(camera.gateFit, cameraSpec2.gateFitMode);
            Assert.AreEqual(camera.sensorSize, cameraSpec2.sensorSize);
        }

        [UnityTest]
        public IEnumerator Effect_DepthOfField_RandomizesProperly()
        {
            var effect = SetupEffectForTag<DepthOfFieldEffect>();
            yield return VolumeComponentValuesDidChange(effect.depthOfField, vc =>
            {
                return new object[]
                {
                    vc.nearFocusStart.value,
                    vc.nearFocusEnd.value,
                    vc.farFocusStart.value,
                    vc.farFocusEnd.value,
                };
            });
        }

        [UnityTest]
        public IEnumerator Effect_Exposure_RandomizesProperly()
        {
            var effect = SetupEffectForTag<ExposureEffect>();
            yield return VolumeComponentValuesDidChange(effect.exposure, vc =>
            {
                return new object[]
                {
                    vc.compensation.value
                };
            });
        }

        [UnityTest]
        public IEnumerator Effect_LensDistortion_RandomizesProperly()
        {
            var effect = SetupEffectForTag<LensDistortionEffect>();
            // by default the center is a ConstantSampler at (0.5, 0.5)
            effect.center = new Vector2Parameter()
            {
                x = new UniformSampler(0f, 1f),
                y = new UniformSampler(0f, 1f)
            };

            yield return VolumeComponentValuesDidChange(effect.lensDistortion, vc =>
            {
                return new object[]
                {
                    vc.intensity.value,
                    vc.center.value,
                    vc.scale.value,
                    vc.xMultiplier.value,
                    vc.yMultiplier.value
                };
            });
        }

        [UnityTest]
        public IEnumerator Effect_MotionBlur_RandomizesProperly()
        {
            var effect = SetupEffectForTag<MotionBlurEffect>();
            yield return VolumeComponentValuesDidChange(effect.motionBlur, vc =>
            {
                return new object[]
                {
                    vc.intensity.value,
                    vc.maximumVelocity.value,
                    vc.minimumVelocity.value
                };
            });
        }
    }
}
#endif
