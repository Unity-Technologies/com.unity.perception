using System;
using UnityEditor.Perception.GroundTruth;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Custom editor for <see cref="TransformRandomizerTag" />.
    /// </summary>
    [CustomEditor(typeof(TransformRandomizerTag))]
    [MovedFrom("UnityEditor.Perception.Internal")]
    class TransformRandomizerTagEditor : ParameterUIElementsEditor
    {
        // SerializedProperties
        SerializedProperty useUniformScale => serializedObject.FindProperty("useUniformScale");
        SerializedProperty shouldRandomizePosition => serializedObject.FindProperty("shouldRandomizePosition");
        SerializedProperty shouldRandomizeRotation => serializedObject.FindProperty("shouldRandomizeRotation");
        SerializedProperty shouldRandomizeScale => serializedObject.FindProperty("shouldRandomizeScale");

        // UXML References
        VisualElement m_Root;
        VisualElement m_PositionContainer;
        VisualElement m_RotationContainer;
        VisualElement m_ScaleContainer;

        Toggle m_UseUniformScale;
        Toggle m_ShouldRandomizePosition;
        Toggle m_ShouldRandomizeRotation;
        Toggle m_ShouldRandomizeScale;

        PropertyField m_Scale;
        PropertyField m_UniformScale;

        void OnEnable()
        {
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{RandomizationLibraryConfiguration.EditorUxmlDirectory}/{nameof(TransformRandomizerTagEditor)}.uxml"
                ).CloneTree();

            m_PositionContainer = m_Root.Q<VisualElement>(name = "positionContainer");
            m_RotationContainer = m_Root.Q<VisualElement>(name = "rotationContainer");
            m_ScaleContainer = m_Root.Q<VisualElement>(name = "scaleContainer");

            m_UseUniformScale = m_Root.Q<Toggle>(name = "useUniformScale");
            m_UseUniformScale.value = useUniformScale.boolValue;

            m_ShouldRandomizePosition = m_Root.Q<Toggle>(name = "shouldRandomizePosition");
            m_ShouldRandomizePosition.value = shouldRandomizePosition.boolValue;
            m_ShouldRandomizeRotation = m_Root.Q<Toggle>(name = "shouldRandomizeRotation");
            m_ShouldRandomizeRotation.value = shouldRandomizeRotation.boolValue;
            m_ShouldRandomizeScale = m_Root.Q<Toggle>(name = "shouldRandomizeScale");
            m_ShouldRandomizeScale.value = shouldRandomizeScale.boolValue;

            m_Scale = m_Root.Q<PropertyField>(name = "scale");
            m_UniformScale = m_Root.Q<PropertyField>(name = "uniformScale");

            m_ShouldRandomizePosition.RegisterCallback<ChangeEvent<bool>>(AnyToggleChanged);
            m_ShouldRandomizeRotation.RegisterCallback<ChangeEvent<bool>>(AnyToggleChanged);
            m_ShouldRandomizeScale.RegisterCallback<ChangeEvent<bool>>(AnyToggleChanged);
            m_UseUniformScale.RegisterCallback<ChangeEvent<bool>>(AnyToggleChanged);
        }

        void AnyToggleChanged(ChangeEvent<bool> evt)
        {
            CreateInspectorGUI();
        }

        /// <summary>
        /// Build the Inspector UI for <see cref="TransformRandomizerTag" />
        /// </summary>
        /// <returns>The root visual element of the Inspector UI</returns>
        public override VisualElement CreateInspectorGUI()
        {
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            m_PositionContainer.SetVisible(m_ShouldRandomizePosition.value);
            m_RotationContainer.SetVisible(m_ShouldRandomizeRotation.value);
            m_ScaleContainer.SetVisible(m_ShouldRandomizeScale.value);

            m_Scale.SetVisible(!m_UseUniformScale.value);
            m_UniformScale.SetVisible(m_UseUniformScale.value);

            UiExtensions.RecursivelyLoadTooltipsFromBoundProperties(m_Root, serializedObject);

            return m_Root;
        }
    }
}
