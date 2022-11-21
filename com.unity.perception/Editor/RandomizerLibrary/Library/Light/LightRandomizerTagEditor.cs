using System;
using UnityEditor.Perception.GroundTruth;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

namespace UnityEditor.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Custom editor for <see cref="LightRandomizerTag" />.
    /// </summary>
    [CustomEditor(typeof(LightRandomizerTag))]
    [MovedFrom("UnityEditor.Perception.Internal")]
    class LightRandomizerTagEditor : ParameterUIElementsEditor
    {
        // Target Object and Properties
        SerializedProperty specifyIntensityAsList => serializedObject.FindProperty("specifyIntensityAsList");
        SerializedProperty specifyTemperatureAsList => serializedObject.FindProperty("specifyTemperatureAsList");
        SerializedProperty specifyColorAsList => serializedObject.FindProperty("specifyColorAsList");

        // UXML References
        VisualElement m_Root;
        Toggle m_SpecifyIntensityAsList;
        PropertyField m_Intensity;
        PropertyField m_IntensityList;

        Toggle m_SpecifyTemperatureAsList;
        PropertyField m_Temperature;
        PropertyField m_TemperatureList;

        Toggle m_SpecifyColorAsList;
        PropertyField m_Color;
        PropertyField m_ColorList;

        void OnEnable()
        {
            // Reference UXML elements
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{RandomizationLibraryConfiguration.EditorUxmlDirectory}/LightRandomizerTagEditor.uxml"
                ).CloneTree();
            m_Root.Bind(serializedObject);

            m_SpecifyIntensityAsList = m_Root.Q<Toggle>(name = "specifyIntensityAsList");
            m_SpecifyIntensityAsList.RegisterCallback<ChangeEvent<bool>>(AnyToggleChanged);
            m_Intensity = m_Root.Q<PropertyField>(name = "intensity");
            m_IntensityList = m_Root.Q<PropertyField>(name = "intensityList");

            m_SpecifyTemperatureAsList = m_Root.Q<Toggle>(name = "specifyTemperatureAsList");
            m_SpecifyTemperatureAsList.RegisterCallback<ChangeEvent<bool>>(AnyToggleChanged);
            m_Temperature = m_Root.Q<PropertyField>(name = "temperature");
            m_TemperatureList = m_Root.Q<PropertyField>(name = "temperatureList");

            m_SpecifyColorAsList = m_Root.Q<Toggle>(name = "specifyColorAsList");
            m_SpecifyColorAsList.RegisterCallback<ChangeEvent<bool>>(AnyToggleChanged);
            m_Color = m_Root.Q<PropertyField>(name = "color");
            m_ColorList = m_Root.Q<PropertyField>(name = "colorList");

            // Load tooltips from bound properties
            UiExtensions.RecursivelyLoadTooltipsFromBoundProperties(m_Root, serializedObject);
        }

        /// <summary>
        /// Anytime a toggle changes value, recreate the UI.
        /// </summary>
        /// <param name="evt">The change event containing the new toggle value</param>
        void AnyToggleChanged(ChangeEvent<bool> evt)
        {
            m_Root.schedule.Execute(() =>
            {
                CreateInspectorGUI();
            });
        }

        /// <summary>
        /// Build the Inspector UI for a <see cref="LightRandomizerTag" />.
        /// </summary>
        /// <returns>The root visual element of the Inspector UI</returns>
        public override VisualElement CreateInspectorGUI()
        {
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            // Show either numeric or categorical parameter for intensity
            m_Intensity.SetVisible(!specifyIntensityAsList.boolValue);
            m_IntensityList.SetVisible((specifyIntensityAsList.boolValue));

            // Show either numeric or categorical parameter for temperature
            m_Temperature.SetVisible(!specifyTemperatureAsList.boolValue);
            m_TemperatureList.SetVisible((specifyTemperatureAsList.boolValue));

            // Show either numeric or categorical parameter for temperature
            m_Color.SetVisible(!specifyColorAsList.boolValue);
            m_ColorList.SetVisible(specifyColorAsList.boolValue);

            return m_Root;
        }
    }
}
