using System;
using UnityEditor;
using UnityEngine;

namespace UnityEngine.Perception.Settings
{
    [CustomPropertyDrawer(typeof(AccumulationSettings))]
    public class AccumulationSettingsDrawer : PropertyDrawer
    {
        const int k_PaddingAmount = 5;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginProperty(position, label, property);

            var settings = property.serializedObject.targetObject as PerceptionSettings;
            var accumulation = settings != null ? settings.accumulationSettings : null;
            var serializedObject = property.serializedObject;

            if (accumulation == null)
            {
                Debug.LogError("Could not find Accumulation Settings, creating a new one.");
                PerceptionSettings.instance.accumulationSettings = new AccumulationSettings()
                {
                    accumulationSamples = 256,
                    shutterInterval = 0,
                    shutterFullyOpen = 0,
                    shutterBeginsClosing = 1,
                    adaptFixedLengthScenarioFrames = true,
                };
                accumulation = settings != null ? settings.accumulationSettings : null;
                serializedObject = property.serializedObject;
            }
            var height = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            var s = new GUIStyle(EditorStyles.boldLabel);
            position.height = height;
            EditorGUI.LabelField(position, ObjectNames.NicifyVariableName(accumulation.GetType().Name), s);
            position.y += height;

            EditorGUI.indentLevel += 1;
            EditorGUIUtility.labelWidth = 250f;
            foreach (SerializedProperty prop in property)
            {
                EditorGUI.PropertyField(position, prop);
                position.y += height + k_PaddingAmount;
            }

            EditorGUI.indentLevel -= 1;

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var count = 0;
            foreach (SerializedProperty y in property)
            {
                count++;
            }
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * (count + 2);
        }
    }
}
