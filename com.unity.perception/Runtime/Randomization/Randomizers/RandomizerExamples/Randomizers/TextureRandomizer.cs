using System;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers.Tags;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Randomizes the material texture of objects tagged with a TextureRandomizerTag
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Texture Randomizer")]
    public class TextureRandomizer : Randomizer
    {
        static readonly int k_BaseMap = Shader.PropertyToID("_BaseMap");
#if HDRP_PRESENT
        const string k_TutorialHueShaderName = "Shader Graphs/HueShiftOpaque";
        static readonly int k_BaseColorMap = Shader.PropertyToID("_BaseColorMap");
#endif

        /// <summary>
        /// The list of textures to sample and apply to target objects
        /// </summary>
        [Tooltip("The list of textures to sample and apply to target objects.")]
        public Texture2DParameter texture;

        /// <summary>
        /// Randomizes the material texture of tagged objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var tags = tagManager.Query<TextureRandomizerTag>();
            foreach (var tag in tags)
            {
                var renderer = tag.GetComponent<Renderer>();
#if HDRP_PRESENT
                // Choose the appropriate shader texture property ID depending on whether the current material is
                // using the default HDRP/lit shader or the Perception tutorial's HueShiftOpaque shader
                var material = renderer.material;
                var propertyId = material.shader.name == k_TutorialHueShaderName ? k_BaseMap : k_BaseColorMap;
                material.SetTexture(propertyId, texture.Sample());
#else
                renderer.material.SetTexture(k_BaseMap, texture.Sample());
#endif
            }
        }
    }
}
