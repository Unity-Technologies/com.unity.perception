using System;
using UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers.Tags;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Sets a gameobject tagged with <see cref="ConstantTransformRandomizerTag"/> to the same transform(location, rotation, scale)
    /// each frame. This randomizer is useful to reset a scene to a start state, especially when used in conjunction
    /// with other randomizers that may move a gameobject.
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Constant Transform Randomizer")]
    public class ConstantTransformRandomizer : Randomizer
    {
        /// <inheritdoc/>
        protected override void OnIterationStart()
        {
            var taggedObjects = tagManager.Query<ConstantTransformRandomizerTag>();
            foreach (var taggedObject in taggedObjects)
            {
                var tag = taggedObject.GetComponent<ConstantTransformRandomizerTag>();
                taggedObject.transform.position = tag.position;
                taggedObject.transform.rotation = tag.rotation;
                taggedObject.transform.localScale = tag.scale;
            }
        }
    }
}
