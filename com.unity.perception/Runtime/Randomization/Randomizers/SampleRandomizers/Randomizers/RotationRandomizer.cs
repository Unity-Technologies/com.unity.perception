using System;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers.Tags;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    [Serializable]
    public class RotationRandomizer : Randomizer
    {
        public Vector3Parameter rotation = new Vector3Parameter();

        protected override void OnIterationStart()
        {
            var taggedObjects = tagManager.Query<RotationRandomizerTag>();
            foreach (var taggedObject in taggedObjects)
                taggedObject.transform.rotation = Quaternion.Euler(rotation.Sample());
        }
    }
}
