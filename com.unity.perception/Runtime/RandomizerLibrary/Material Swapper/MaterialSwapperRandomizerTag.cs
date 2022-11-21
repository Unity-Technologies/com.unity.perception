using System;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Randomizes the target material of an object when used in conjunction with <see cref="MaterialSwapperRandomizer"/>.
    /// </summary>
    [AddComponentMenu("Perception/RandomizerTags/Material Swapper Tag")]
    [MovedFrom("UnityEngine.Perception.Internal")]
    [RequireComponent(typeof(Renderer))]
    public class MaterialSwapperRandomizerTag : RandomizerTag
    {
        /// <summary>
        /// The index of the target material element (i.e. material tied to a submesh)
        /// </summary>
        [Tooltip("The material element which will be randomized from the options below.")]
        public int targetedMaterialIndex = 0;
        /// <summary>
        /// The list of materials from which the target material will be replaced
        /// </summary>
        [Tooltip("Randomly chooses a material from the options provided and assigns it to the submesh/material element specified above. The probability of each material being selected can be modified by disabling the Uniform flag and providing probability values manually.")]
        public CategoricalParameter<Material> materials = new CategoricalParameter<Material>();

        Renderer m_Renderer;
        /// <summary>
        /// The Renderer component attached to the GameObject which has the <see cref="MaterialSwapperRandomizerTag" />.
        /// </summary>
        public Renderer Renderer => m_Renderer = m_Renderer ? m_Renderer : GetComponent<Renderer>();

        /// <summary>
        /// For the selected material element (whose index in the materials array is given
        /// by <see cref="targetedMaterialIndex"/>), sample a material from <see cref="materials" /> and set it as the
        /// above material element's material.
        /// </summary>
        public void Randomize()
        {
            if (materials.Count <= 0)
                return;

            // Whether intentional or not, "Renderer.materials[i] = xyz" does not work. The entire array must be assigned
            // instead. One potential way would be to cache the materials array at the start, however this does not
            // support multiple MaterialSwapperRandomizerTag's as there would be multiple disparate caches. Our best bet
            // is to get the materials array each time we want to randomize, modify it, and reassign it.
            var tempMaterials = Renderer.materials;
            tempMaterials[targetedMaterialIndex] = materials.Sample();
            Renderer.materials = tempMaterials;
        }
    }
}
