using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Parameters.Attributes;

namespace UnityEngine.Perception.Randomization.Editor
{
    [CustomPropertyDrawer(typeof(Parameter), true)]
    public class ParameterDrawer : PropertyDrawer
    {
        bool m_Cached;
        List<Parameter> m_Parameters;
        string[] m_Options;
        int m_SelectedOptionIndex;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!m_Cached)
            {
                var parameterType = fieldInfo.FieldType;

                m_Parameters = new List<Parameter>();

                if (parameterType == typeof(Parameter))
                    m_Parameters = Resources.FindObjectsOfTypeAll<Parameter>().ToList();
                else
                {
                    var genericParameters = Resources.FindObjectsOfTypeAll<Parameter>();
                    foreach (var parameter in genericParameters)
                    {
                        if (parameter.GetType() == parameterType)
                            m_Parameters.Add(parameter);
                    }
                }

                m_Options = new string[m_Parameters.Count + 1];
                m_Options[0] = "None";
                for (var i = 1; i <= m_Parameters.Count; i++)
                {
                    var parameter = m_Parameters[i - 1];
                    var metadata = ParameterMetaData.GetMetaData(parameter.GetType());
                    m_Options[i] = $"{parameter.parameterName} ({metadata.typeDisplayName})";
                }

                var selectedParameter = property.objectReferenceValue;
                if (selectedParameter != null)
                {
                    for (var i = 0; i < m_Parameters.Count; i++)
                        if (m_Parameters[i].GetInstanceID() == selectedParameter.GetInstanceID())
                        {
                            m_SelectedOptionIndex = i + 1;
                            break;
                        }
                }
                else
                    m_SelectedOptionIndex = 0;
            }
            m_Cached = true;

            EditorGUI.BeginProperty(position, label, property);
            var originalOption = m_SelectedOptionIndex;
            m_SelectedOptionIndex = EditorGUI.Popup(position, fieldInfo.Name, m_SelectedOptionIndex, m_Options);
            if (originalOption != m_SelectedOptionIndex)
            {
                property.objectReferenceValue = m_SelectedOptionIndex == 0
                    ? null
                    : m_Parameters[m_SelectedOptionIndex - 1];
            }
            EditorGUI.EndProperty();
        }
    }
}
