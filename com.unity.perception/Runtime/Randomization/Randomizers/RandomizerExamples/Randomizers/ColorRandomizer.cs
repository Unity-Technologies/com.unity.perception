using System;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers.Tags;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Randomizes the material color of objects tagged with a ColorRandomizerTag
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Color Randomizer")]
    public class ColorRandomizer : Randomizer
    {
        static readonly int k_BaseColor = Shader.PropertyToID("_BaseColor");

        /// <summary>
        /// Describes the range of random colors to assign to tagged objects
        /// </summary>
        public ColorHsvaParameter colorParameter;

        /// <summary>
        /// Randomizes the colors of tagged objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var taggedObjects = tagManager.Query<ColorRandomizerTag>();
            foreach (var taggedObject in taggedObjects)
            {
                var renderer = taggedObject.GetComponent<Renderer>();
                renderer.material.SetColor(k_BaseColor, colorParameter.Sample());
            }
        }
    }
}
