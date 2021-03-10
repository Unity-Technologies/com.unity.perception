using UnityEngine;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Interface for setting up Renderers for ground truth generation via <see cref="LabelManager"/>.
    /// </summary>
    public interface IGroundTruthGenerator
    {
        /// <summary>
        /// Called by <see cref="LabelManager"/> when first registered or when a Labeling is created at runtime.
        /// </summary>
        /// <param name="mpb">The MaterialPropertyBlock for the given meshRenderer. Can be used to set properties for custom rendering.</param>
        /// <param name="renderer">The Renderer under the given Labeling.</param>
        /// <param name="labeling">The Labeling component created</param>
        /// <param name="instanceId">The instanceId assigned to the given Labeling instance.</param>
        void SetupMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId);
    }
}
