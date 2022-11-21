#if SUBSTANCE_PLUGIN_ENABLED
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting.APIUpdating;
using Substance.Game;
using UnityEditor.Perception.Randomization;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Custom editor for <see cref="SubstanceRandomizerTag" />.
    /// </summary>
    [CustomEditor(typeof(SubstanceRandomizerTag))]
    [MovedFrom("UnityEditor.Perception.Internal")]
    class SubstanceRandomizerTagEditor : ParameterUIElementsEditor
    {
        #region UXML Reference
        SubstanceRandomizerTag m_Target;
        SerializedProperty parametersToRandomize => serializedObject.FindProperty("parametersToRandomize");

        // UXML References
        VisualElement m_Root;
        VisualElement m_ParameterPopupContainer;
        VisualElement m_ParameterControlContainer;
        Label m_NoParametersWarning;
        PopupField<SubstanceGraph.InputProperties> m_ParameterOptions;
        ListView m_ParameterList;
        Button m_ClearParametersButton;
        Button m_NewParameterButton;
        Label m_PausedNotice;

        List<SubstanceGraph.InputProperties> m_CachedProperties;
        #endregion

        #region UI Events
        /// <summary>
        /// Remove a specific parameter from the list of parameters to randomize.
        /// </summary>
        void DeleteParameterAtIndex(int index)
        {
            parametersToRandomize.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Removes all parameters set up for randomization.
        /// </summary>
        void DeleteAllParameters()
        {
            parametersToRandomize.ClearArray();
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Adds the currently selected shader parameter to the list of parameters for randomization.
        /// </summary>
        void AddSelectedParameter()
        {
            serializedObject.Update();
            var addedParameter = m_ParameterOptions.value;

            if (m_Target.parametersToRandomize.Contains(addedParameter.name))
                return;

            var newItemIndex = parametersToRandomize.arraySize;
            parametersToRandomize.InsertArrayElementAtIndex(newItemIndex);
            parametersToRandomize.GetArrayElementAtIndex(newItemIndex).stringValue = addedParameter.name;
            serializedObject.ApplyModifiedProperties();

            CreateInspectorGUI();
        }

        #endregion

        #region Implicity Unity Events
        void OnEnable()
        {
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{RandomizationLibraryConfiguration.EditorUxmlDirectory}/{nameof(SubstanceRandomizerTagEditor)}.uxml"
                ).CloneTree();
            m_Target = serializedObject.targetObject as SubstanceRandomizerTag;

            m_ParameterList = m_Root.Q<ListView>(name = "parameters");
            var initialList = new List<string>();
            if (m_Target != null && (m_Target.parametersToRandomize?.Count ?? 0) > 0)
                initialList = m_Target.parametersToRandomize;
            m_ParameterList.itemsSource = initialList;

            // How each parameter list item looks like
            m_ParameterList.makeItem = () => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{RandomizationLibraryConfiguration.EditorUxmlDirectory}/SubstanceParameterListItem.uxml").CloneTree();
            m_ParameterList.bindItem = (e, i) =>
            {
                e.Q<Label>(name = "item_label").text = m_Target.parametersToRandomize[i];
                e.Q<Button>(name = "delete_button").RegisterCallback<MouseUpEvent>(evt =>
                {
                    DeleteParameterAtIndex(i);
                    CreateInspectorGUI();
                });
            };
            m_ParameterList.selectionType = SelectionType.None;

            m_ParameterControlContainer = m_Root.Q<VisualElement>(name = "parameter_control_container");
            m_ParameterPopupContainer = m_Root.Q<VisualElement>(name = "parameter_popup_container");
            m_ClearParametersButton = m_Root.Q<Button>(name = "clear_parameters");
            m_NewParameterButton = m_Root.Q<Button>(name = "add_parameter");
            m_NoParametersWarning = m_Root.Q<Label>(name = "no_items_warning");

            m_NewParameterButton.RegisterCallback<MouseUpEvent>(evt =>
            {
                AddSelectedParameter();
                CreateInspectorGUI();
            });
            m_ClearParametersButton.RegisterCallback<MouseUpEvent>(evt =>
            {
                DeleteAllParameters();
                CreateInspectorGUI();
            });

            m_PausedNotice = new Label("Substance Tag Editor is paused during play mode.");
            m_PausedNotice.AddToClassList("rlib__message.info");
            m_PausedNotice.AddToClassList("info");
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return m_PausedNotice;

            serializedObject.Update();

            // Load all existing parameters
            m_ParameterList.itemsSource = m_Target.parametersToRandomize;
            m_ParameterList.Refresh();

            // Parameters which can be added exclude ones that have been already added
            // Note: m_CachedProperties is used as m_Target.graph.GetInputProperties() throws an exception briefly
            // as we exit playmode (perhaps when substances are being regenerated).
            var availableProperties = new List<SubstanceGraph.InputProperties>();
            try
            {
                availableProperties = m_Target.graph.GetInputProperties()
                    .Where(prop => !m_Target.parametersToRandomize.Contains(prop.name) && prop.name != "$outputsize")
                    .ToList();
                m_CachedProperties = availableProperties;
            }
            finally
            {
                m_CachedProperties = m_CachedProperties ?? new List<SubstanceGraph.InputProperties>();
            }

            // Do we have at least 1 parameter added?
            var hasExistingParameters = (m_Target.parametersToRandomize?.Count ?? 0) > 0;
            // Do we have more parameters we can add?
            var hasMoreAddableParameters = availableProperties.ToList().Count > 0;

            if (hasMoreAddableParameters)
            {
                m_ParameterOptions = new PopupField<SubstanceGraph.InputProperties>(
                    availableProperties.ToList(),
                    0,
                    formatSelectedValueCallback: prop => $"{(string.IsNullOrWhiteSpace(prop.@group) ? "" : $"{prop.@group}/")}{prop.name} ({prop.type.ToString()})",
                    formatListItemCallback: prop => $"{(string.IsNullOrWhiteSpace(prop.@group) ? "" : $"{prop.@group}/")}{prop.name} ({prop.type.ToString()})"
                );
                m_ParameterPopupContainer.Clear();
                m_ParameterPopupContainer.Add(m_ParameterOptions);
            }

            m_ParameterControlContainer.SetVisible(hasMoreAddableParameters);
            m_NoParametersWarning.SetVisible(!hasExistingParameters);
            m_ClearParametersButton.SetVisible(hasExistingParameters);
            m_NewParameterButton.SetVisible(hasMoreAddableParameters);

            UiExtensions.RecursivelyLoadTooltipsFromBoundProperties(m_Root, serializedObject);

            return m_Root;
        }

        #endregion
    }
}
#endif
