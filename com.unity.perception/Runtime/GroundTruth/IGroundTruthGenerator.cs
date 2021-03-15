using UnityEngine;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Interface for setting up Renderers for ground truth generation via <see cref="LabelManager"/>.
    /// </summary>
    public interface IGroundTruthGenerator
    {
        /// <summary>
        /// Called by <see cref="LabelManager"/> when a <see cref="Labeling"/> component is first registered, created at runtime, or enabled at runtime.
        /// </summary>
        /// <param name="mpb">The MaterialPropertyBlock for the given <see cref="MeshRenderer"/>. Can be used to set properties for custom rendering.</param>
        /// <param name="renderer">The <see cref="Renderer"/> under the given <see cref="LabelManager"/>.</param>
        /// <param name="labeling">The <see cref="LabelManager"/> component that was registered, created, or enabled</param>
        /// <param name="instanceId">The instanceId assigned to the given <see cref="LabelManager"/> instance.</param>
        void SetupMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId);

        /// <summary>
        /// Called by <see cref="LabelManager"/> when a <see cref="Labeling"/> component is disabled, causing it to not be included in ground-truth generation.
        /// </summary>
        /// <param name="mpb">The MaterialPropertyBlock for the given <see cref="MeshRenderer"/>. Can be used to set properties for custom rendering.</param>
        /// <param name="renderer">The <see cref="Renderer"/> under the given <see cref="LabelManager"/>.</param>
        /// <param name="labeling">The <see cref="LabelManager"/> component for which ground-truth generation should stop.</param>
        /// <param name="instanceId">The instanceId assigned to the given <see cref="LabelManager"/> instance.</param>
        void ClearMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId);
    }
}
