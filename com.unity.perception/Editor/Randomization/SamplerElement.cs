using System;
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
        SerializedObject m_SerializedObject;
        SerializedObject m_ParameterSo;
        VisualElement m_Properties;
        ToolbarMenu m_SamplerTypeDropdown;

        public SamplerElement(SerializedProperty property)
        {
            m_Property = property;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{StaticData.uxmlDir}/SamplerElement.uxml");
            template.CloneTree(this);

            m_Sampler = (Sampler)m_Property.objectReferenceValue;
            m_ParameterSo = property.serializedObject;
            m_Parameter = (Parameter)m_ParameterSo.targetObject;

            if (m_Sampler == null)
            {
                m_Sampler = m_Parameter.gameObject.AddComponent<UniformSampler>();
                ((UniformSampler)m_Sampler).adrFloat.baseRandomSeed = (uint)Random.Range(1, int.MaxValue);
                m_Sampler.hideFlags = HideFlags.HideInInspector;
                m_Property.objectReferenceValue = m_Sampler;
                m_ParameterSo.ApplyModifiedProperties();
            }

            m_SerializedObject = new SerializedObject(m_Sampler);

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
            var newSampler = m_Parameter.gameObject.AddComponent(samplerType);
            if (samplerType.IsSubclassOf(typeof(RandomSampler)))
                ((RandomSampler)newSampler).adrFloat.baseRandomSeed = (uint)Random.Range(1, int.MaxValue);

            newSampler.hideFlags = HideFlags.HideInInspector;
            m_Property.objectReferenceValue = newSampler;
            m_ParameterSo.Dispose();
            Object.DestroyImmediate(m_Sampler);
            m_ParameterSo.ApplyModifiedProperties();

            m_SamplerTypeDropdown.text = m_Sampler.MetaData.displayName;
            m_ParameterSo = new SerializedObject(newSampler);
            CreatePropertyFields();
        }

        static string UppercaseFirstLetter(string s)
        {
            return string.IsNullOrEmpty(s) ? string.Empty : char.ToUpper(s[0]) + s.Substring(1);
        }

        void CreatePropertyFields()
        {
            m_Properties.Clear();
            var iterator = m_SerializedObject.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.propertyPath == "m_Script")
                        continue;
                    switch (iterator.type)
                    {
                        case "AdrFloat":
                            m_Properties.Add(new AdrFloatElement(iterator.Copy()));
                            break;
                        default:
                        {
                            var propertyField = new PropertyField(iterator.Copy());
                            propertyField.Bind(m_SerializedObject);
                            m_Properties.Add(propertyField);
                            break;
                        }
                    }
                } while (iterator.NextVisible(false));
            }
        }
    }
}
