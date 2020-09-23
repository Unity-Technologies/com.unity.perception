using System;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers.Tags;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    [Serializable]
    public class ColorRandomizer : Randomizer
    {
        static readonly int k_BaseColor = Shader.PropertyToID("_BaseColor");
        public ColorHsvaParameter colorParameter;

        protected override void OnIterationStart()
        {
            var taggedObjects = tagManager.Query<ColorRandomizerTag>();
            foreach (var taggedObject in taggedObjects)
            {
                var renderer = taggedObject.GetComponent<MeshRenderer>();
                renderer.material.SetColor(k_BaseColor, colorParameter.Sample());
            }
        }
    }
}
