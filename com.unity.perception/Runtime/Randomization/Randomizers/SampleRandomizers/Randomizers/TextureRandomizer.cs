using System;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers.Tags;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    [Serializable]
    public class TextureRandomizer : Randomizer
    {
        static readonly int k_BaseTexture = Shader.PropertyToID("_BaseMap");
        public Texture2DParameter texture;

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
