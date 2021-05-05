using UnityEngine;

namespace UnityEngine.Perception.Randomization
{
    public abstract class ArchetypeBase
    {
        public abstract string label { get; }

        public abstract void PreprocessAsset(Object asset);
    }
}
