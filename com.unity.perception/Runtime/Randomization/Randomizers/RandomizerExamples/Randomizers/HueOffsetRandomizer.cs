using System;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.Tags;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Randomly offsets the hue of objects tagged with a ColorRandomizerTag
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Hue Offset Randomizer")]
    [MovedFrom("UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers")]
    public class HueOffsetRandomizer : Randomizer
    {
        static readonly int k_HueOffsetShaderProperty = Shader.PropertyToID("_HueOffset");

        /// <summary>
        /// The range of random hue offsets to assign to target objects
        /// </summary>
        [Tooltip("The range of random hue offsets to assign to target objects.")]
        public FloatParameter hueOffset = new FloatParameter { value = new UniformSampler(-180f, 180f) };

        /// <summary>
        /// Randomizes the hue offset of tagged objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var tags = tagManager.Query<HueOffsetRandomizerTag>();
            foreach (var tag in tags)
            {
                var renderer = tag.GetComponent<MeshRenderer>();
                renderer.material.SetFloat(k_HueOffsetShaderProperty, hueOffset.Sample());
            }
        }
    }
}
