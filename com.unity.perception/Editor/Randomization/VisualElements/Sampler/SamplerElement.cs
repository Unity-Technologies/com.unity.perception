using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Samplers;
using UnityEngine.UIElements;

namespace UnityEngine.Experimental.Perception.Randomization.Editor
{
    class SamplerElement : VisualElement
    {
        Parameter m_Parameter;
        ISampler m_Sampler;
        SerializedProperty m_Property;
        SerializedProperty m_RangeProperty;
        VisualElement m_Properties;
        ToolbarMenu m_SamplerTypeDropdown;

        public SamplerElement(SerializedProperty property, Parameter parameter)
        {
            m_Property = property;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/Sampler/SamplerElement.uxml");
            template.CloneTree(this);

            m_Parameter = parameter;
            m_Sampler = GetSamplerFromSerializedObject();

            if (m_Sampler == null)
                CreateSampler(typeof(UniformSampler));

            var samplerName = this.Q<Label>("sampler-name");
            samplerName.text = UppercaseFirstLetter(m_Property.name);

            m_Properties = this.Q<VisualElement>("fields-container");
            m_SamplerTypeDropdown = this.Q<ToolbarMenu>("sampler-type-dropdown");
            m_SamplerTypeDropdown.text = SamplerUtility.GetSamplerDisplayName(m_Sampler.GetType());;
            foreach (var samplerType in StaticData.samplerTypes)
            {
                var displayName = SamplerUtility.GetSamplerDisplayName(samplerType);;
                m_SamplerTypeDropdown.menu.AppendAction(
                    displayName,
                    a => { ReplaceSampler(samplerType); },
                    a => DropdownMenuAction.Status.Normal);
            }
            CreatePropertyFields();
        }

        void ReplaceSampler(Type samplerType)
        {
            CreateSampler(samplerType);
            m_SamplerTypeDropdown.text = SamplerUtility.GetSamplerDisplayName(samplerType);
            CreatePropertyFields();
        }

        void CreateSampler(Type samplerType)
        {
            var newSampler = (ISampler)Activator.CreateInstance(samplerType);

            if (m_RangeProperty != null)
                newSampler.range = new FloatRange(
                    m_RangeProperty.FindPropertyRelative("minimum").floatValue,
                    m_RangeProperty.FindPropertyRelative("maximum").floatValue);


            m_Sampler = newSampler;
            m_Property.managedReferenceValue = newSampler;
            m_Property.serializedObject.ApplyModifiedProperties();
        }

        void CreatePropertyFields()
        {
            m_RangeProperty = null;
            m_Properties.Clear();
            var currentProperty = m_Property.Copy();
            var nextSiblingProperty = m_Property.Copy();
            nextSiblingProperty.NextVisible(false);

            if (currentProperty.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;
                    if (currentProperty.type == "FloatRange")
                    {
                        m_RangeProperty = currentProperty.Copy();
                        m_Properties.Add(new FloatRangeElement(m_RangeProperty));
                    }
                    else
                    {
                        var propertyField = new PropertyField(currentProperty.Copy());
                        propertyField.Bind(m_Property.serializedObject);
                        m_Properties.Add(propertyField);
                    }
                }
                while (currentProperty.NextVisible(false));
            }
        }

        static string UppercaseFirstLetter(string s)
        {
            return string.IsNullOrEmpty(s) ? string.Empty : char.ToUpper(s[0]) + s.Substring(1);
        }

        ISampler GetSamplerFromSerializedObject()
        {
            var configType = m_Parameter.GetType();
            var field = configType.GetField(m_Property.name);
            return (ISampler)field.GetValue(m_Parameter);
        }
    }
}
