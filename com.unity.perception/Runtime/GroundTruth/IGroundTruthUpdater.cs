namespace UnityEngine.Perception.GroundTruth
{
    public interface IGroundTruthUpdater
    {
        void OnBeginUpdate(int count);
        void OnUpdateEntity(Labeling labeling, GroundTruthInfo groundTruthInfo);
        void OnEndUpdate();
    }
}
