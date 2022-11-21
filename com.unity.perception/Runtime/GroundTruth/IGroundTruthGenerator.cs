using UnityEngine;
using UnityEngine.Perception.GroundTruth.LabelManagement;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Interface for setting up Renderers for ground truth generation via <see cref="LabelManager"/>.
    /// </summary>
    public interface IGroundTruthGenerator
    {
        /// <summary>
        /// Enables ground truth generation for a <see cref="Labeling"/> component or its associated <see cref="MaterialPropertyBlock"/>. This function is called by <see cref="LabelManager"/> when a <see cref="Labeling"/> component is registered, created, or enabled.
        /// </summary>
        /// <param name="mpb">The <see cref="MaterialPropertyBlock"/> for the given <see cref="MeshRenderer"/>. Can be used to set properties for custom rendering.</param>
        /// <param name="renderer">The <see cref="Renderer"/> under the given <see cref="LabelManager"/>.</param>
        /// <param name="labeling">The <see cref="LabelManager"/> component that was registered, created, or enabled</param>
        /// <param name="material">The specific material on the Renderer that the MaterialPropertyBlock will be applied to.</param>
        /// <param name="instanceId">The instanceId assigned to the given <see cref="LabelManager"/> instance.</param>
        void SetupMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, Material material, uint instanceId);

        /// <summary>
        /// Disables ground truth generation for a <see cref="Labeling"/> component or its associated <see cref="MaterialPropertyBlock"/>. This function is called by <see cref="LabelManager"/> when a <see cref="Labeling"/> component is disabled.
        /// </summary>
        /// <param name="mpb">The <see cref="MaterialPropertyBlock"/> for the given <see cref="MeshRenderer"/>. Can be used to set properties for custom rendering.</param>
        /// <param name="renderer">The <see cref="Renderer"/> under the given <see cref="LabelManager"/>.</param>
        /// <param name="labeling">The <see cref="LabelManager"/> component for which ground-truth generation should stop.</param>
        /// <param name="instanceId">The instanceId assigned to the given <see cref="LabelManager"/> instance.</param>
        void ClearMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId);
    }
}
