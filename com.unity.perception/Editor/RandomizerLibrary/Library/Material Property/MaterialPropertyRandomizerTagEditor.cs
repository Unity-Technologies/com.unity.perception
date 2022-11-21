using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Perception.GroundTruth;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Utilities;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Custom editor for <see cref="MaterialPropertyRandomizerTag" />.
    /// </summary>
    [CustomEditor(typeof(MaterialPropertyRandomizerTag))]
    [MovedFrom("UnityEditor.Perception.Internal")]
    class MaterialPropertyRandomizerTagEditor : ParameterUIElementsEditor
    {
        /// <summary>
        /// State Machine for <see cref="MaterialPropertyRandomizerTagEditor" />.
        /// </summary>
        enum EditorState
        {
            Uninitialized,
            InvalidMaterialConfiguration,
            PartialShaderPropertyList,
            FilledShaderPropertyList
        }

        #region Properties & UXML References
        /// <summary>
        /// The index of the material element whose material will be randomized
        /// </summary>
        SerializedProperty targetMaterialIndex => serializedObject.FindProperty("targetedMaterialIndex");
        /// <summary>
        /// The list of <see cref="RLibShaderProperty" /> which will be randomized at runtime.
        /// </summary>
        SerializedProperty propertiesToRandomize => serializedObject.FindProperty("propertiesToRandomize");

        MaterialPropertyRandomizerTag m_Target;

        // State
        EditorState m_State = EditorState.Uninitialized;

        // UXML References
        VisualElement m_Root;
        Label m_TopWarning;
        VisualElement m_TargetMaterialContainer;
        VisualElement m_MaterialContainer;
        VisualElement m_ShaderContainer;
        ListView m_PropertiesList;
        VisualElement m_ShaderPropertyOptionsContainer;
        VisualElement m_ShaderPropertyAttachPoint;
        Button m_ClearAllButton;
        Button m_AddButton;
        PopupField<MaterialPropertyEntry> m_TargetMaterialPopup;
        PopupField<ShaderPropertyEntry> m_ShaderPropertyPopup;
        #endregion

        #region Helper Functions
        /// <summary>
        /// For the target object, gets all available material elements name along with the index through which it is
        /// accessible in the materials property of the target object's <see cref="Renderer"/>
        /// </summary>
        /// <returns>A list of <see cref="RLibMaterialProperty"/></returns>
        List<MaterialPropertyEntry> GetMaterials()
        {
            // Note: We use sharedMaterials as accessing materials during edit-time can leak the material.
            var sharedMaterials = m_Target.Renderer.sharedMaterials;
            if (sharedMaterials.Length <= 0)
                return new List<MaterialPropertyEntry>();

            return sharedMaterials.Select((mat, index) =>
            {
                if (mat == null)
                    return null;

                return new MaterialPropertyEntry()
                {
                    name = mat.name, index = index
                };
            }).ToList();
        }

        /// <summary>
        /// For the target object, gets the name, description, and type of all appropriate shader properties associated
        /// with the material element at <see cref="targetMaterialIndex"/>.
        /// </summary>
        /// <returns>A list of <see cref="RLibShaderProperty"/></returns>
        /// <remarks>
        /// By "appropriate," we mean shader properties which have not been marked
        /// as <see cref="ShaderPropertyFlags.NonModifiableTextureData" />
        /// </remarks>
        List<ShaderPropertyEntry> GetAllValidShaderProperties()
        {
            var shaderProps = new List<ShaderPropertyEntry>();
            var selectedMaterial = m_Target.targetMaterial;
            if (selectedMaterial == null)
                return shaderProps;

            var selectedShader = selectedMaterial.shader;
            if (selectedShader == null)
                return shaderProps;

            for (var i = 0; i < selectedShader.GetPropertyCount(); i++)
            {
                var shaderRepresentation = ShaderPropertyEntry.FromShaderPropertyIndex(selectedShader, i);
                if (shaderRepresentation != null)
                    shaderProps.Add(shaderRepresentation);
            }

            shaderProps.Sort((x, y) => x.index.CompareTo(y.index));
            return shaderProps;
        }

        #endregion

        #region UI Updates

        void SetState(EditorState newState, bool doNotRefreshUI = false)
        {
            if (m_State == newState)
                return;
            m_State = newState;
            if (!doNotRefreshUI)
                CreateInspectorGUI();
        }

        void OnMaterialChoiceUpdated(ChangeEvent<MaterialPropertyEntry> evt)
        {
            serializedObject.Update();
            targetMaterialIndex.intValue = evt.newValue.index;
            serializedObject.ApplyModifiedProperties();
            RemoveAllShaderProperties();
            SetupShaderPropertyPopup();
            CreateInspectorGUI();
        }

        /// <summary>
        /// Updates the items in the popup field (that allows a user to select the targeted material element) and
        /// reattaches it to the container visual element.
        /// </summary>
        void SetupMaterialChoicePopup()
        {
            // Create the material popup
            var sharedMaterials = GetMaterials();
            var hasAtLeastOneInvalidMaterial = sharedMaterials.Any(mat => mat == null);
            if (sharedMaterials.Count == 0 || hasAtLeastOneInvalidMaterial)
            {
                SetState(EditorState.InvalidMaterialConfiguration);
                return;
            }

            // ** Invariant: There is at least one material and all materials are valid. **

            // Remove container element from the hierarchy
            if (m_TargetMaterialContainer.childCount > 0)
                m_TargetMaterialContainer.RemoveAt(0);

            // Make sure target material index is valid
            var clampedTargetMaterialIndex = Mathf.Clamp(targetMaterialIndex.intValue, 0, sharedMaterials.Count - 1);
            targetMaterialIndex.intValue = clampedTargetMaterialIndex;

            var targetMaterial = m_Target.Renderer.sharedMaterials[clampedTargetMaterialIndex];
            var newMaterialName = targetMaterial.name;

            serializedObject.ApplyModifiedProperties();

            // Update Target Material Popup
            m_TargetMaterialPopup?.UnregisterValueChangedCallback(OnMaterialChoiceUpdated);
            m_TargetMaterialPopup = new PopupField<MaterialPropertyEntry>(
                "Target Material",
                sharedMaterials,
                clampedTargetMaterialIndex,
                formatListItemCallback: prop => $"{prop.name} ({prop.index})",
                formatSelectedValueCallback: prop => prop.name
            );
            m_TargetMaterialPopup?.RegisterValueChangedCallback(OnMaterialChoiceUpdated);
            m_TargetMaterialPopup.tooltip = "The material element which will be randomized from the options below.";

            // Insert popup into container element
            m_TargetMaterialContainer.Insert(0, m_TargetMaterialPopup);

            SetState(EditorState.PartialShaderPropertyList);
        }

        void SetupShaderPropertyPopup()
        {
            serializedObject.Update();

            var allShaderProperties = GetAllValidShaderProperties();
            var addedProperties = m_Target.propertiesToRandomize ?? new List<ShaderPropertyEntry>();
            var availableProperties = allShaderProperties
                .Where(prop => !addedProperties.Contains(prop))
                .ToList();
            availableProperties.Sort((x, y) => x.name.CompareTo(y.name));

            if (availableProperties.Count > 0)
            {
                m_ShaderPropertyPopup = new PopupField<ShaderPropertyEntry>(
                    "Available Properties",
                    availableProperties,
                    0,
                    formatListItemCallback: prop => $"{prop.description.Replace(@"/", "|")} ({prop.SupportedShaderPropertyType().ToString()})",
                    formatSelectedValueCallback: prop => prop.name
                    ) { tooltip = "The shader property to be added to the material randomization." };
                m_ShaderPropertyAttachPoint.Clear();
                m_ShaderPropertyAttachPoint.Insert(0, m_ShaderPropertyPopup);
                SetState(EditorState.PartialShaderPropertyList);
            }
            else
            {
                SetState(EditorState.FilledShaderPropertyList);
            }

            m_PropertiesList.itemsSource = addedProperties;
            m_PropertiesList.Rebuild();
            m_AddButton.userData = (addedProperties.Count, allShaderProperties.Count);

            serializedObject.ApplyModifiedProperties();
        }

        #region Event Callbacks
        void AddSelectedShaderProperty()
        {
            serializedObject.Update();

            var newShaderProperty = m_ShaderPropertyPopup?.value;
            if (newShaderProperty == null)
                return;

            var newShaderPropertyIndex = propertiesToRandomize.arraySize;
            propertiesToRandomize.InsertArrayElementAtIndex(newShaderPropertyIndex);
            var selectedProperty = propertiesToRandomize.GetArrayElementAtIndex(newShaderPropertyIndex);
            selectedProperty.managedReferenceValue = newShaderProperty;

            serializedObject.ApplyModifiedProperties();

            SetupShaderPropertyPopup();
        }

        void RemoveAllShaderProperties()
        {
            if (propertiesToRandomize.isArray && propertiesToRandomize.arraySize > 0)
            {
                propertiesToRandomize.ClearArray();
                serializedObject.ApplyModifiedProperties();
            }
        }

        void PropertyListItemBound(VisualElement itemTemplate, int i)
        {
            var selectedShaderProperty = m_Target.propertiesToRandomize[i];

            // To support alternating colors
            var container = itemTemplate.Q<VisualElement>(name = "shader_property_drawer");
            container.RemoveFromClassList("odd_row");
            container.RemoveFromClassList("even_row");
            container.AddToClassList(i % 2 == 1 ? "odd_row" : "even_row");

            var propertyName = itemTemplate.Q<Label>(name = "property_info");
            propertyName.text = $"Property: {selectedShaderProperty.name} ({selectedShaderProperty.description})";

            var propertyDeleteButton = itemTemplate.Q<Button>(name = "delete_button");
            propertyDeleteButton.UnregisterCallback<MouseUpEvent>(PropertyListItemDeleteButtonClicked);
            propertyDeleteButton.userData = i;
            propertyDeleteButton.RegisterCallback<MouseUpEvent>(PropertyListItemDeleteButtonClicked);

            var parameterPropertyUI = itemTemplate.Q<PropertyField>(name = "parameter_property");
            parameterPropertyUI.bindingPath = "parameter";
            var parameterProperty = propertiesToRandomize.FindPropertyRelative($"Array.data[{i}].parameter");
            parameterPropertyUI.BindProperty(parameterProperty);
        }

        void PropertyListItemDeleteButtonClicked(MouseUpEvent evt)
        {
            var button = evt.target as Button;
            if (button?.userData is int i)
            {
                if (propertiesToRandomize.arraySize > i && i >= 0)
                {
                    serializedObject.Update();
                    propertiesToRandomize.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                }

                SetupShaderPropertyPopup();
            }
        }

        void UndoRedoPerformed()
        {
            CreateInspectorGUI();
        }

        #endregion
        #endregion

        #region Unity Implicit Functions
        void OnEnable()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            m_Target = serializedObject.targetObject as MaterialPropertyRandomizerTag;
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{RandomizationLibraryConfiguration.EditorUxmlDirectory}/MaterialPropertyRandomizerTagEditor.uxml"
                ).CloneTree();

            // Get UXML References
            m_TopWarning = m_Root.Q<Label>(name = "top_warning");
            m_TopWarning.style.display = DisplayStyle.None;

            m_MaterialContainer = m_Root.Q<VisualElement>(name = "material_container");
            m_ShaderContainer = m_Root.Q<VisualElement>(name = "shader_container");
            m_TargetMaterialContainer = m_Root.Q<VisualElement>(name = "material_choice_container");
            m_ShaderPropertyAttachPoint = m_Root.Q<VisualElement>(name = "parameter_popup_container");
            m_ShaderPropertyOptionsContainer = m_Root.Q<VisualElement>(name = "parameter_control_container");
            // Buttons
            m_AddButton = m_Root.Q<Button>(name = "add_parameter");
            m_AddButton.RegisterCallback<MouseUpEvent>((evt =>
            {
                AddSelectedShaderProperty();
                CreateInspectorGUI();
            }));
            m_ClearAllButton = m_Root.Q<Button>(name = "clear_parameters");
            m_ClearAllButton.RegisterCallback<MouseUpEvent>((evt =>
            {
                RemoveAllShaderProperties();
                CreateInspectorGUI();
            }));

            // Initialize ListView
            m_PropertiesList = m_Root.Q<ListView>(name = "properties");
            m_PropertiesList.makeItem = () => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{RandomizationLibraryConfiguration.EditorUxmlDirectory}/RLibShaderPropertyDrawer.uxml"
                ).CloneTree();
            m_PropertiesList.bindItem = PropertyListItemBound;
            m_PropertiesList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            Undo.undoRedoPerformed += UndoRedoPerformed;

            SetupMaterialChoicePopup();
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        /// <summary>
        /// Build the Inspector UI for <see cref="MaterialPropertyRandomizerTag" />.
        /// </summary>
        /// <returns>The root visual element of the Inspector UI</returns>
        public override VisualElement CreateInspectorGUI()
        {
            if (EditorApplication.isPlaying)
            {
                return new Label("Editing not supported in playmode.");
            }

            serializedObject.Update();

            if (m_State == EditorState.Uninitialized || m_State == EditorState.InvalidMaterialConfiguration)
            {
                m_TopWarning.text = $"Please make sure the Renderer attached to this GameObject has at least one material and none of the materials are set to None.";
                m_TopWarning.SetVisible(true);
                m_MaterialContainer.SetVisible(false);
                m_ShaderContainer.SetVisible(false);
            }

            if (m_State == EditorState.PartialShaderPropertyList || m_State == EditorState.FilledShaderPropertyList)
            {
                m_TopWarning.SetVisible(propertiesToRandomize.arraySize <= 0);
                m_MaterialContainer.SetVisible(true);
                m_ShaderContainer.SetVisible(true);
                SetupShaderPropertyPopup();
                m_ShaderPropertyOptionsContainer.SetVisible(true);

                if (m_State == EditorState.FilledShaderPropertyList)
                {
                    m_ShaderPropertyOptionsContainer.SetVisible(false);
                }
            }

            UiExtensions.RecursivelyLoadTooltipsFromBoundProperties(m_Root, serializedObject);
            return m_Root;
        }

        #endregion
    }
}
