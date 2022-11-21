using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RandomizerTests.Internal;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using PerceptionParameters = UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Perception.Utilities;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace RandomizerTests
{
    [TestFixture]
    public class MaterialPropertyRandomizerTests
    {
        const int k_HdrpLitShaderBaseColorPropertyIndex = 0; // Color
        const int k_HdrpLitShaderBaseColorMapPropertyIndex = 1; // Texture
        const int k_HdrpLitShaderBaseColorMapMipInfoPropertyIndex = 2; // Vector
        const int k_HdrpLitShaderMetallicPropertyIndex = 3; // Range
        const int k_HdrpLitShaderHeightAmplitudePropertyIndex = 18; // Float

        /// <remarks>
        /// We get this from the prefab that the <see cref="TestUtils" /> class uses when setting up a tag for the scene.
        /// </remarks>
        const string k_ExpectedMaterialName = "Red Material";

        FixedLengthScenario m_Scenario;
        MaterialPropertyRandomizer m_Randomizer;
        MaterialPropertyRandomizerTag m_Tag;

        [SetUp]
        public void Setup()
        {
            TestUtils.SetupRandomizerTestScene<MaterialPropertyRandomizer, MaterialPropertyRandomizerTag>(
                ref m_Scenario, ref m_Randomizer, ref m_Tag,
                ((scenario, randomizer, tag) =>
                {
                    scenario.enabled = false;
                })
            );
        }

        [TearDown]
        public void Teardown()
        {
            TestUtils.ResetScene();
        }

        [UnityTest]
        public IEnumerator ShaderProperty_MappedProperlyToType()
        {
            var(mat, shader) = VerifySetup();
            var colorSp = ShaderPropertyEntry.FromShaderPropertyIndex(shader, k_HdrpLitShaderBaseColorPropertyIndex) as ColorShaderPropertyEntry;
            var textureSp = ShaderPropertyEntry.FromShaderPropertyIndex(shader, k_HdrpLitShaderBaseColorMapPropertyIndex) as TextureShaderPropertyEntry;
            var vectorSp = ShaderPropertyEntry.FromShaderPropertyIndex(shader, k_HdrpLitShaderBaseColorMapMipInfoPropertyIndex) as VectorPropertyEntry;
            var floatSp = ShaderPropertyEntry.FromShaderPropertyIndex(shader, k_HdrpLitShaderHeightAmplitudePropertyIndex) as FloatShaderPropertyEntry;
            var rangeSp = ShaderPropertyEntry.FromShaderPropertyIndex(shader, k_HdrpLitShaderMetallicPropertyIndex) as RangeShaderPropertyEntry;

            Assert.IsTrue(
                colorSp != null &&
                textureSp != null &&
                vectorSp != null &&
                floatSp != null &&
                rangeSp != null
            );

            Assert.AreEqual(ShaderPropertyType.Color, colorSp.SupportedShaderPropertyType());
            Assert.AreEqual(ShaderPropertyType.Texture, textureSp.SupportedShaderPropertyType());
            Assert.AreEqual(ShaderPropertyType.Vector, vectorSp.SupportedShaderPropertyType());
            Assert.AreEqual(ShaderPropertyType.Float, floatSp.SupportedShaderPropertyType());
            Assert.AreEqual(ShaderPropertyType.Range, rangeSp.SupportedShaderPropertyType());

            yield return null;
        }

        [UnityTest]
        public IEnumerator ShaderProperty_Color_ProperlyRandomizes()
        {
            // Set initial values for properties
            var(mat, shader) = VerifySetup();

            var initialValue = Color.magenta;
            var valueToRandomizeTo = Color.green;

            // 1. Color
            var colorSp = ShaderPropertyEntry.FromShaderPropertyIndex(shader, k_HdrpLitShaderBaseColorPropertyIndex) as ColorShaderPropertyEntry;
            Assert.NotNull(colorSp);
            mat.SetColor(colorSp.name, initialValue);

            // Verify initial values were set
            Assert.AreEqual(mat.GetColor(colorSp.name), initialValue);

            // Set randomization parameters for the shader property
            colorSp.parameter.SetOptions(new List<Color>()
            {
                valueToRandomizeTo
            });

            // Run the randomizations
            yield return RunScene(colorSp);

            // Test whether the shader properties were properly randomized
            Assert.AreEqual(mat.GetColor(colorSp.name), valueToRandomizeTo);
        }

        [UnityTest]
        public IEnumerator ShaderProperty_Texture_ProperlyRandomizes()
        {
            // Set initial values for properties
            var(mat, shader) = VerifySetup();

            var initialValue = Texture2D.blackTexture;
            var valueToRandomizeTo = Texture2D.redTexture;

            // 2. Texture
            var textureSp = ShaderPropertyEntry.FromShaderPropertyIndex(shader, k_HdrpLitShaderBaseColorMapPropertyIndex) as TextureShaderPropertyEntry;
            Assert.NotNull(textureSp);
            mat.SetTexture(textureSp.name, initialValue);

            // Verify initial values were set
            Assert.AreEqual(mat.GetTexture(textureSp.name), initialValue);

            // Set randomization parameters for the shader property
            textureSp.parameter.SetOptions(new[]
            {
                valueToRandomizeTo
            });

            // Run the randomizations
            yield return RunScene(textureSp);

            // Test whether the shader properties were properly randomized
            Assert.AreEqual(mat.GetTexture(textureSp.name), valueToRandomizeTo);
        }

        [UnityTest]
        public IEnumerator ShaderProperty_Vector_ProperlyRandomizes()
        {
            // Set initial values for properties
            var(mat, shader) = VerifySetup();

            var initialValue = Vector4.zero;
            var valueToRandomizeTo = Vector4.one;

            // 3. Vector
            var vectorSp = ShaderPropertyEntry.FromShaderPropertyIndex(shader, k_HdrpLitShaderBaseColorMapMipInfoPropertyIndex) as VectorPropertyEntry;
            Assert.NotNull(vectorSp);
            mat.SetVector(vectorSp.name, initialValue);

            // Verify initial values were set
            Assert.AreEqual(mat.GetVector(vectorSp.name), initialValue);

            // Set randomization parameters for the shader property
            vectorSp.parameter.x = new ConstantSampler(valueToRandomizeTo.x);
            vectorSp.parameter.y = new ConstantSampler(valueToRandomizeTo.y);
            vectorSp.parameter.z = new ConstantSampler(valueToRandomizeTo.z);
            vectorSp.parameter.w = new ConstantSampler(valueToRandomizeTo.w);

            // Run the randomizations
            yield return RunScene(vectorSp);

            // Test whether the shader properties were properly randomized
            Assert.AreEqual(mat.GetVector(vectorSp.name), valueToRandomizeTo);
        }

        [UnityTest]
        public IEnumerator ShaderProperty_Float_ProperlyRandomizes()
        {
            // Set initial values for properties
            var(mat, shader) = VerifySetup();

            const float initialValue = 0f;
            const float valueToRandomizeTo = 1f;

            // 4. Float
            var floatSp = ShaderPropertyEntry.FromShaderPropertyIndex(shader, k_HdrpLitShaderHeightAmplitudePropertyIndex) as FloatShaderPropertyEntry;
            Assert.NotNull(floatSp);
            mat.SetFloat(floatSp.name, initialValue);

            // Verify initial values were set
            Assert.AreEqual(mat.GetFloat(floatSp.name), initialValue);

            // Set randomization parameters for the shader property
            floatSp.parameter.value = new ConstantSampler(valueToRandomizeTo);

            // Run the randomizations
            yield return RunScene(floatSp);

            // Test whether the shader properties were properly randomized
            Assert.AreEqual(mat.GetFloat(floatSp.name), valueToRandomizeTo);
        }

        [UnityTest]
        public IEnumerator ShaderProperty_Range_ProperlyRandomizes()
        {
            // Set initial values for properties
            var(mat, shader) = VerifySetup();

            const float initialValue = 0f;
            const float valueToRandomizeTo = 1f;

            // 5. Range
            var rangeSp = ShaderPropertyEntry.FromShaderPropertyIndex(shader, k_HdrpLitShaderMetallicPropertyIndex) as RangeShaderPropertyEntry;
            Assert.NotNull(rangeSp);
            mat.SetFloat(rangeSp.name, initialValue);

            // Verify initial values were set
            Assert.AreEqual(mat.GetFloat(rangeSp.name), initialValue);

            // Set randomization parameters for the shader property
            rangeSp.parameter.value = new ConstantSampler(valueToRandomizeTo);

            // Run the randomizations
            yield return RunScene(rangeSp);

            // Test whether the shader properties were properly randomized
            Assert.AreEqual(mat.GetFloat(rangeSp.name), valueToRandomizeTo);
        }

        IEnumerator RunScene(params ShaderPropertyEntry[] prop)
        {
            m_Tag.propertiesToRandomize = prop.ToList();
            m_Scenario.enabled = true;
            yield return null;
            yield return null;
            m_Scenario.enabled = false;
        }

        /// <summary>
        /// Verifies that the right material and shader are set on the test object
        /// </summary>
        (Material mat, Shader shader) VerifySetup()
        {
            m_Tag.targetedMaterialIndex = 0; // Default HD Material as m_Tag is a primitive GameObject.
            var defaultHdMaterial = m_Tag.GetComponent<MeshRenderer>().material;
            // Hard-code to test on the DefaultHDMaterial
            Assert.AreEqual($"{k_ExpectedMaterialName} (Instance)", defaultHdMaterial.name);
            var defaultHdShader = defaultHdMaterial.shader;
            Assert.AreEqual("HDRP/Lit", defaultHdShader.name);

            return (defaultHdMaterial, defaultHdShader);
        }
    }
}
