namespace UnityEngine.Perception.Randomization
{
    public abstract class Archetype<T> : ArchetypeBase where T : Object
    {
        public abstract void Preprocess(T item);

        public override void PreprocessAsset(Object asset)
        {
            Preprocess((T)asset);
        }
    }
}
