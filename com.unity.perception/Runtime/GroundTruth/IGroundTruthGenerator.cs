using UnityEngine;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Interface for setting up MeshRenderers for ground truth generation via <see cref="GroundTruthLabelSetupSystem"/>.
    /// </summary>
    public interface IGroundTruthGenerator
    {
        /// <summary>
        /// Called by <see cref="GroundTruthLabelSetupSystem"/> when first registered or when a Labeling is created at runtime.
        /// </summary>
        /// <param name="mpb">The MaterialPropertyBlock for the given meshRenderer. Can be used to set properties for custom rendering.</param>
        /// <param name="meshRenderer">The MeshRenderer which exists under the given Labeling.</param>
        /// <param name="labeling">The Labeling component created</param>
        /// <param name="instanceId">The instanceId assigned to the given Labeling instance.</param>
        void SetupMaterialProperties(MaterialPropertyBlock mpb, MeshRenderer meshRenderer, Labeling labeling, uint instanceId);
    }
}
