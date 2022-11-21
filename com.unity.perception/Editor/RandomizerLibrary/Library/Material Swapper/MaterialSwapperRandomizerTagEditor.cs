using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Perception.GroundTruth;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Utilities;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Custom editor for <see cref="MaterialSwapperRandomizerTag" />.
    /// </summary>
    [CustomEditor(typeof(MaterialSwapperRandomizerTag))]
    [MovedFrom("UnityEditor.Perception.Internal")]
    class MaterialSwapperRandomizerTagEditor : ParameterUIElementsEditor
    {
        MaterialSwapperRandomizerTag m_Target;
        /// <summary>
        /// The index of the material element whose material will be randomized
        /// </summary>
        SerializedProperty m_TargetMaterialIndex;

        VisualElement m_Root;
        PopupField<MaterialPropertyEntry> m_MaterialChoice;
        PropertyField m_Materials;

        void OnEnable()
        {
            m_Target = serializedObject.targetObject as MaterialSwapperRandomizerTag;

            m_Root = new VisualElement();
            m_Root.Bind(serializedObject);

            m_TargetMaterialIndex = serializedObject.FindProperty("targetedMaterialIndex");
            UpdateMaterialChoice();
            m_Materials = new PropertyField(serializedObject.FindProperty("materials"));
        }

        /// <summary>
        /// For the target object, gets all available material elements name along with the index through which it is
        /// accessible in the materials property of the target object's <see cref="Renderer"/>
        /// </summary>
        /// <returns>A list of <see cref="RLibMaterialProperty"/></returns>
        List<MaterialPropertyEntry> GetMaterials()
        {
            serializedObject.Update();
            var materials = m_Target.Renderer.sharedMaterials;

            if (materials.Length <= 0)
            {
                Debug.LogError($"Material Error: No materials found in the Renderer component of {m_Target.name}.");
                return new List<MaterialPropertyEntry>();
            }

            return materials.Select((mat, index) => new MaterialPropertyEntry() {
                name = mat.name, index = index
            }).ToList();
        }

        /// <summary>
        /// Updates the items in the popup field (that allows a user to select the targeted material element.)
        /// </summary>
        void UpdateMaterialChoice()
        {
            var sharedMaterials = GetMaterials();
            m_MaterialChoice?.UnregisterCallback<ChangeEvent<MaterialPropertyEntry>>(OnMaterialChoiceUpdated);
            m_MaterialChoice = new PopupField<MaterialPropertyEntry>(
                "Target Material",
                sharedMaterials,
                m_TargetMaterialIndex.intValue,
                formatListItemCallback: prop => $"{prop.name} ({prop.index})",
                formatSelectedValueCallback: prop => prop.name
            );
            m_MaterialChoice.tooltip = "The material element which will be randomized from the options below.";
            m_MaterialChoice.RegisterCallback<ChangeEvent<MaterialPropertyEntry>>(OnMaterialChoiceUpdated);
        }

        /// <summary>
        /// When a new material element is selected in the popup, updates the corresponding property for the
        /// corresponding serialized object.
        /// </summary>
        /// <param name="evt">A change event with the new <see cref="RLibMaterialProperty"/></param>
        void OnMaterialChoiceUpdated(ChangeEvent<MaterialPropertyEntry> evt)
        {
            m_TargetMaterialIndex.intValue = evt.newValue.index;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Build the Inspector UI for <see cref="MaterialSwapperRandomizerTag" />.
        /// </summary>
        /// <returns>The root visual element of the Inspector UI</returns>
        public override VisualElement CreateInspectorGUI()
        {
            m_Root.Clear();
            serializedObject.Update();
            m_Root.Add(m_MaterialChoice);
            m_Root.Add(m_Materials);

            // Load tooltips from bound properties
            UiExtensions.RecursivelyLoadTooltipsFromBoundProperties(m_Root, serializedObject);

            return m_Root;
        }
    }
}
