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
                CreateSampler(typeof(UniformSampler));

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
            m_SerializedObject.Dispose();
            Object.DestroyImmediate(m_Sampler);
            CreateSampler(samplerType);
            m_SamplerTypeDropdown.text = m_Sampler.MetaData.displayName;
            m_SerializedObject = new SerializedObject(m_Sampler);
            CreatePropertyFields();
        }

        void CreateSampler(Type samplerType)
        {
            m_Sampler = (Sampler)m_Parameter.gameObject.AddComponent(samplerType);
            m_Sampler.hideFlags = HideFlags.HideInInspector;
            if (samplerType.IsSubclassOf(typeof(RandomSampler)))
                ((RandomSampler)m_Sampler).seed = (uint)Random.Range(1, int.MaxValue);

            m_Property.objectReferenceValue = m_Sampler;
            m_ParameterSo.ApplyModifiedProperties();
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
                    if (iterator.propertyPath == "seed")
                    {
                        var seedField = new IntegerField("Seed");
                        seedField.BindProperty(iterator.Copy());
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
                    switch (iterator.type)
                    {
                        case "FloatRange":
                            m_Properties.Add(new FloatRangeElement(iterator.Copy()));
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

        static string UppercaseFirstLetter(string s)
        {
            return string.IsNullOrEmpty(s) ? string.Empty : char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
