using System;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers.Tags;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Randomly offsets the hue of objects tagged with a ColorRandomizerTag
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Hue Offset Randomizer")]
    public class HueOffsetRandomizer : Randomizer
    {
        static readonly int k_HueOffsetShaderProperty = Shader.PropertyToID("_HueOffset");

        /// <summary>
        /// The range of hue offsets to assign to tagged objects
        /// </summary>
        public FloatParameter hueOffset = new FloatParameter { value = new UniformSampler(-180f, 180f) };

        /// <summary>
        /// Randomizes the hue offset of tagged objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var taggedObjects = tagManager.Query<HueOffsetRandomizerTag>();
            foreach (var taggedObject in taggedObjects)
            {
                var renderer = taggedObject.GetComponent<MeshRenderer>();
                renderer.material.SetFloat(k_HueOffsetShaderProperty, hueOffset.Sample());
            }
        }
    }
}
