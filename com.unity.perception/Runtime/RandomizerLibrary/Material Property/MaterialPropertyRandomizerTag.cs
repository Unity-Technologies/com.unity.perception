using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Perception.Utilities;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Randomizes the shader properties of an object when used in conjunction with <see cref="MaterialPropertyRandomizer"/>.
    /// </summary>
    [AddComponentMenu("Perception/RandomizerTags/Material Property Randomizer Tag")]
    [Serializable]
    [RequireComponent(typeof(Renderer))]
    [MovedFrom("UnityEngine.Perception.Internal")]
    public class MaterialPropertyRandomizerTag : RandomizerTag
    {
        /// <summary>
        /// The index of the material element whose shader properties will be randomized based on the options below.
        /// </summary>
        [Tooltip("The index of the material element whose shader properties will be randomized based on the options below.")]
        public int targetedMaterialIndex = 0;
        /// <summary>
        /// The shader properties which will be randomized on the selected material element.
        /// </summary>
        [Tooltip("The shader properties which will be randomized on the selected material element.")]
        [SerializeReference]
        public List<ShaderPropertyEntry> propertiesToRandomize;

        // Helper Properties
        /// <summary>
        /// Gets the material selected in the UI.
        /// </summary>
        public Material targetMaterial => attachedMaterialsCount > targetedMaterialIndex ? Renderer.sharedMaterials[targetedMaterialIndex] : null;
        /// <summary>
        /// Number of shared material elements which are not set to None.
        /// </summary>
        public int attachedMaterialsCount => Renderer.sharedMaterials?.Where(mat => mat != null).ToList().Count ?? 0;


        Renderer m_Renderer;
        /// <summary>
        /// The Renderer component attached to the GameObject which has the <see cref="MaterialSwapperRandomizerTag" />.
        /// </summary>
        public Renderer Renderer => m_Renderer = m_Renderer ? m_Renderer : GetComponent<Renderer>();

        Material[] m_MaterialsCache;

        /// <summary>
        /// Randomizes the shader properties specified in <see cref="propertiesToRandomize" />
        /// </summary>
        public void Randomize()
        {
            if (attachedMaterialsCount <= 0 || propertiesToRandomize.Count <= 0)
                return;

            m_MaterialsCache = Renderer.materials;
            var cachedTargetMaterial = m_MaterialsCache[targetedMaterialIndex];

            foreach (var property in propertiesToRandomize)
            {
                var propID = Shader.PropertyToID(property.name);
                switch (property)
                {
                    case FloatShaderPropertyEntry fProp:
                        cachedTargetMaterial.SetFloat(propID, fProp.parameter.Sample());
                        break;
                    case TextureShaderPropertyEntry tProp:
                        cachedTargetMaterial.SetTexture(propID, tProp.parameter.Sample());
                        break;
                    case ColorShaderPropertyEntry cProp:
                        cachedTargetMaterial.SetColor(propID, cProp.parameter.Sample());
                        break;
                    case RangeShaderPropertyEntry rProp:
                        cachedTargetMaterial.SetFloat(propID, rProp.parameter.Sample());
                        break;
                    case VectorPropertyEntry vProp:
                        cachedTargetMaterial.SetVector(propID, vProp.parameter.Sample());
                        break;
                }
            }

            Renderer.materials = m_MaterialsCache;
        }
    }
}
