using System;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers.Tags;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
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
        /// The range of random colors to assign to target objects
        /// </summary>
        [Tooltip("The range of random colors to assign to target objects.")]
        public ColorHsvaParameter colorParameter;

        /// <summary>
        /// Randomizes the colors of tagged objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var tags = tagManager.Query<ColorRandomizerTag>();
            foreach (var tag in tags)
            {
                var renderer = tag.GetComponent<Renderer>();
                renderer.material.SetColor(k_BaseColor, colorParameter.Sample());
            }
        }
    }
}
