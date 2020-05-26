using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    public class ParameterDrawerElement : BindableElement
    {
        SerializedProperty m_Property;
        FieldInfo m_FieldInfo;
        ToolbarMenu m_ParameterMenu;

        public ParameterDrawerElement(SerializedProperty property, FieldInfo fieldInfo)
        {
            m_Property = property;
            m_FieldInfo = fieldInfo;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ParameterDrawerElement.uxml");
            template.CloneTree(this);

            var propertyLabel = this.Q<Label>();
            propertyLabel.text = property.displayName;
            m_ParameterMenu = this.Q<ToolbarMenu>();
            binding = new ParameterDrawerBinding(this);
        }

        string GetSelectedOptionText()
        {
            var parameter = m_Property.objectReferenceValue as Parameter;
            if (parameter == null)
                return "None";
            return DisplayName(parameter);
        }

        static string DisplayName(Parameter parameter)
        {
            return $"{parameter.parameterName} ({parameter.MetaData.typeDisplayName})";
        }

        void UpdateMenuOptions()
        {
            m_ParameterMenu.menu.MenuItems().Clear();
            var parameters = GatherParameterOptions();
            var options = GetStringOptions(parameters);
            for (var i = 0; i < options.Length; i++)
            {
                var index = i;
                var option = options[i];
                m_ParameterMenu.menu.AppendAction(option, action =>
                {
                    m_Property.objectReferenceValue = option == "None" ? null : parameters[index - 1];
                    m_Property.serializedObject.ApplyModifiedProperties();
                    m_ParameterMenu.text = GetSelectedOptionText();
                });
            }
        }

        Parameter[] GatherParameterOptions()
        {
            var parameterType = m_FieldInfo.FieldType;
            var parameters = new List<Parameter>();

            if (parameterType == typeof(Parameter))
                parameters = Resources.FindObjectsOfTypeAll<Parameter>().ToList();
            else
            {
                var genericParameters = Resources.FindObjectsOfTypeAll<Parameter>();
                foreach (var parameter in genericParameters)
                {
                    if (parameter.GetType() == parameterType)
                        parameters.Add(parameter);
                }
            }
            parameters.Sort((p1, p2) => p1.parameterName.CompareTo(p2.parameterName));

            return parameters.ToArray();
        }

        string[] GetStringOptions(Parameter[] parameters)
        {
            var options = new string[parameters.Length + 1];
            options[0] = "None";
            for (var i = 1; i <= parameters.Length; i++)
            {
                var parameter = parameters[i - 1];
                var metadata = ParameterMetaData.GetMetaData(parameter.GetType());
                options[i] = $"{parameter.parameterName} ({metadata.typeDisplayName})";
            }
            return options;
        }

        class ParameterDrawerBinding : IBinding
        {
            ParameterDrawerElement m_Element;

            public ParameterDrawerBinding(ParameterDrawerElement element) => m_Element = element;

            public void PreUpdate() { }

            public void Update()
            {
                m_Element.UpdateMenuOptions();
                m_Element.m_ParameterMenu.text = m_Element.GetSelectedOptionText();
            }

            public void Release() { }
        }
    }
}
