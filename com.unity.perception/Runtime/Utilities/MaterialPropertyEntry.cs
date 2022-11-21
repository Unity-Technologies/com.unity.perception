using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Utilities
{
    /// <summary>
    /// A struct containing the name and index of a material element for a given Renderer.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.Internal")]
    class MaterialPropertyEntry
    {
        /// <summary>
        /// The name of the material (eg: "ConstructionLights")
        /// </summary>
        public string name;
        /// <summary>;
        /// The index of the material element for a specific GameObjects Renderer. When the GameObject has only one
        /// mesh (no submeshes), then index will always be 0 i.e. the first material element.
        /// </summary>
        public int index;
    }
}
