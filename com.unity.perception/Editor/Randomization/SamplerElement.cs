﻿using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    public class SamplerElement : VisualElement
    {
        Parameter m_Parameter;
        Sampler m_Sampler;
        SerializedProperty m_Property;
        SerializedObject m_ParameterSo;
        VisualElement m_Properties;
        ToolbarMenu m_SamplerTypeDropdown;

        public SamplerElement(SerializedProperty property)
        {
            m_Property = property;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{StaticData.uxmlDir}/SamplerElement.uxml");
            template.CloneTree(this);

            m_ParameterSo = property.serializedObject;
            m_Parameter = (Parameter)m_ParameterSo.targetObject;
            m_Sampler = GetSamplerFromSerializedObject();

            if (m_Sampler == null)
                CreateSampler(typeof(UniformSampler));

            var samplerName = this.Q<Label>("sampler-name");
            samplerName.text = UppercaseFirstLetter(m_Property.propertyPath);

            m_Properties = this.Q<VisualElement>("fields-container");
            m_SamplerTypeDropdown = this.Q<ToolbarMenu>("sampler-type-dropdown");
            m_SamplerTypeDropdown.text = m_Sampler.MetaData.displayName;
            foreach (var samplerType in StaticData.samplerTypes)
            {
                var displayName = SamplerMetaData.GetMetaData(samplerType).displayName;
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
            m_SamplerTypeDropdown.text = m_Sampler.MetaData.displayName;
            CreatePropertyFields();
        }

        void CreateSampler(Type samplerType)
        {
            var newSampler = (Sampler)Activator.CreateInstance(samplerType);
            if (samplerType.IsSubclassOf(typeof(RandomSampler)))
                ((RandomSampler)m_Sampler).seed = (uint)Random.Range(1, int.MaxValue);

            if (m_Sampler != null &&
                m_Sampler is RangedSampler oldRangedSampler &&
                newSampler is RangedSampler newRangedSampler)
                newRangedSampler.range = oldRangedSampler.range;

            m_Sampler = newSampler;

            m_Property.managedReferenceValue = m_Sampler;
            m_ParameterSo.ApplyModifiedProperties();
        }

        void CreatePropertyFields()
        {
            m_Properties.Clear();
            var currentProperty = m_Property.Copy();
            var nextSiblingProperty = m_Property.Copy();
            nextSiblingProperty.Next(false);

            if (currentProperty.Next(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;
                    if (currentProperty.name == "seed")
                    {
                        var seedField = new IntegerField("Seed");
                        seedField.BindProperty(currentProperty.Copy());
                        seedField.RegisterValueChangedCallback((e) =>
                        {
                            if (e.newValue <= 0)
                            {
                                seedField.value = 0;
                                seedField.binding.Update();
                                e.StopImmediatePropagation();
                            }
                        });
                        m_Properties.Add(seedField);
                        continue;
                    }
                    switch (currentProperty.type)
                    {
                        case "FloatRange":
                            m_Properties.Add(new FloatRangeElement(currentProperty.Copy()));
                            break;
                        default:
                        {
                            var propertyField = new PropertyField(currentProperty.Copy());
                            propertyField.Bind(m_Property.serializedObject);
                            m_Properties.Add(propertyField);
                            break;
                        }
                    }
                }
                while (currentProperty.Next(false));
            }
        }

        static string UppercaseFirstLetter(string s)
        {
            return string.IsNullOrEmpty(s) ? string.Empty : char.ToUpper(s[0]) + s.Substring(1);
        }

        Sampler GetSamplerFromSerializedObject()
        {
            var propertyPath = m_Property.propertyPath;
            var parameterType = m_Parameter.GetType();
            return (Sampler)parameterType.GetField(propertyPath).GetValue(m_Parameter);
        }
    }
}
