﻿using System;
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
#if HDRP_PRESENT
        static readonly int k_BaseTexture = Shader.PropertyToID("_BaseColorMap");
#else
        static readonly int k_BaseTexture = Shader.PropertyToID("_BaseMap");
#endif

        /// <summary>
        /// The list of textures to sample and apply to tagged objects
        /// </summary>
        public Texture2DParameter texture;

        /// <summary>
        /// Randomizes the material texture of tagged objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var tags = tagManager.Query<TextureRandomizerTag>();
            foreach (var tag in tags)
            {
                var renderer = tag.GetComponent<MeshRenderer>();
                renderer.material.SetTexture(k_BaseTexture, texture.Sample());
            }
        }
    }
}
