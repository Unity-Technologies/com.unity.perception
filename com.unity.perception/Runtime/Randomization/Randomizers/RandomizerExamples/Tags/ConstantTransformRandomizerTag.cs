namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers.Tags
{
    /// <summary>
    /// Tag to indicate which gameobjects should have the effects of <see cref="ConstantTransformRandomizer"/> applied
    /// to them.
    /// </summary>
    [AddComponentMenu("Perception/RandomizerTags/Constant Transform Randomizer Tag")]
    public class ConstantTransformRandomizerTag : RandomizerTag
    {
        public Vector3 position = Vector3.zero;
        public Quaternion rotation;
        public Vector3 scale = Vector3.one;
    }
}
