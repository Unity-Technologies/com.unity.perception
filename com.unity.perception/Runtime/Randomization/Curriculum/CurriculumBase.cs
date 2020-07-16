namespace UnityEngine.Perception.Randomization.Curriculum
{
    public abstract class CurriculumBase
    {
        public abstract string Type { get; }
        public abstract int CurrentIteration { get; }
        public abstract bool FinishedIterating { get; }

        public abstract void Initialize();

        public abstract void Iterate();
    }
}
