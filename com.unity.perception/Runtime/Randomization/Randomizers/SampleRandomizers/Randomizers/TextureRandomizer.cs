using System;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers.Tags;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Randomizes the material texture of objects tagged with a TextureRandomizerTag
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Texture Randomizer")]
    public class TextureRandomizer : Randomizer
    {
        static readonly int k_BaseTexture = Shader.PropertyToID("_BaseMap");

        /// <summary>
        /// The list of textures to sample and apply to tagged objects
        /// </summary>
        public Texture2DParameter texture;

        /// <summary>
        /// Randomizes the material texture of tagged objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var taggedObjects = tagManager.Query<TextureRandomizerTag>();
            foreach (var taggedObject in taggedObjects)
            {
                var renderer = taggedObject.GetComponent<MeshRenderer>();
                renderer.material.SetTexture(k_BaseTexture, texture.Sample());
            }
        }
    }
}
