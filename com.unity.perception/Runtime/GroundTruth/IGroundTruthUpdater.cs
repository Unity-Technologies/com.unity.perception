namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    ///  Interface for labeled components which need to produce their ground truth info each frame.
    /// </summary>
    public interface IGroundTruthUpdater
    {
        /// <summary>
        /// Initial call of the labeling pass. Called to notify labeler that a new
        /// set of component updates are starting.
        /// </summary>
        void OnBeginUpdate();

        /// <summary>
        /// The middle pass of an update set. Called immediately after <see cref="OnBeginUpdate"/>.
        /// This method may be called several times before <see cref="OnEndUpdate"/> is called, because
        /// it will be called once for each labeled component in the scene.
        /// </summary>
        /// <param name="labeling">The <see cref="Labeling"/> of the component.</param>
        /// <param name="groundTruthInfo">The <see cref="GroundTruthInfo"/> of the component.</param>
        void OnUpdateEntity(Labeling labeling, GroundTruthInfo groundTruthInfo);

        /// <summary>
        /// The final call of a ground truth updater pass. Informs the labeler that the pass
        /// has completed.
        /// </summary>
        void OnEndUpdate();
    }
}
