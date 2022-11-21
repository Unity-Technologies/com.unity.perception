using System;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization
{
    class SamplerInterfaceElement : VisualElement
    {
        VisualElement m_PropertiesContainer;
        SerializedProperty m_Property;
        SerializedProperty m_RangeProperty;
        ToolbarMenu m_SamplerTypeDropdown;

        ISampler sampler => (ISampler)StaticData.GetManagedReferenceValue(m_Property);

        public SamplerInterfaceElement(SerializedProperty property)
        {
            m_Property = property;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/Sampler/SamplerInterfaceElement.uxml");
            template.CloneTree(this);

            if (sampler == null)
                CreateSampler(typeof(UniformSampler));

            var samplerName = this.Q<Label>("sampler-name");
            samplerName.text = m_Property.displayName;

            m_PropertiesContainer = this.Q<VisualElement>("fields-container");
            m_SamplerTypeDropdown = this.Q<ToolbarMenu>("sampler-type-dropdown");
            m_SamplerTypeDropdown.text = SamplerUtility.GetSamplerDisplayName(sampler.GetType());

            foreach (var samplerType in StaticData.samplerTypes)
            {
                var displayName = SamplerUtility.GetSamplerDisplayName(samplerType);
                ;
                m_SamplerTypeDropdown.menu.AppendAction(
                    displayName,
                    a => ReplaceSampler(samplerType),
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
            CopyFloatRangeToNewSampler(newSampler);
            m_Property.managedReferenceValue = newSampler;
            m_Property.serializedObject.ApplyModifiedProperties();
        }

        void CopyFloatRangeToNewSampler(ISampler newSampler)
        {
            if (m_RangeProperty == null)
                return;

            var rangeField = newSampler.GetType().GetField(m_RangeProperty.name);
            if (rangeField == null)
                return;

            var range = new FloatRange(
                m_RangeProperty.FindPropertyRelative("minimum").floatValue,
                m_RangeProperty.FindPropertyRelative("maximum").floatValue);
            rangeField.SetValue(newSampler, range);
        }

        void CreatePropertyFields()
        {
            m_RangeProperty = null;
            m_PropertiesContainer.Clear();
            UIElementsEditorUtilities.CreatePropertyFields(m_Property, m_PropertiesContainer);
        }
    }
}
