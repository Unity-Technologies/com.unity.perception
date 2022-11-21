using System;
using System.Linq;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Randomizes the shader properties of objects tagged with a <see cref="MaterialPropertyRandomizerTag"/>
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Material Property Randomizer")]
    [MovedFrom("UnityEngine.Perception.Internal")]
    public class MaterialPropertyRandomizer : Randomizer
    {
        /// <summary>
        /// OnIterationStart is called at the start of a new Scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            // Randomize the target material
            tagManager.Query<MaterialPropertyRandomizerTag>()
                .ToList().ForEach(tag => tag.Randomize());
        }
    }
}
