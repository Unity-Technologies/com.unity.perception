using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.Configuration;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Samplers.Editor
{
    [CustomEditor(typeof(ParameterConfiguration))]
    public class ParameterConfigurationEditor : UnityEditor.Editor
    {
        static Type[] s_ParameterTypes;
        static Type[] s_SamplerTypes;

        ParameterConfiguration m_Config;
        VisualElement m_Root;
        VisualElement m_ParameterContainer;
        VisualElement m_ScenarioContainer;
        VisualTreeAsset m_ParameterTemplate;
        VisualTreeAsset m_SamplerTemplate;
        VisualTreeAsset m_AdrFloatTemplate;

        const string k_TemplatesFolder = "Packages/com.unity.perception/Editor/Randomization/Uxml";

        static ParameterConfigurationEditor()
        {
            GatherParameterAndSamplerTypes();
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_Config = (ParameterConfiguration)target;
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{k_TemplatesFolder}/ParameterConfiguration.uxml").CloneTree();
            m_ParameterTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{k_TemplatesFolder}/Parameter.uxml");
            m_SamplerTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{k_TemplatesFolder}/Sampler.uxml");
            m_AdrFloatTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{k_TemplatesFolder}/AdrFloat.uxml");

            m_ParameterContainer = m_Root.Query<VisualElement>("parameter-container").First();
            m_ScenarioContainer = m_Root.Query<VisualElement>("configuration-container");

            foreach (var parameter in m_Config.parameters)
            {
                AddNewParameterToContainer(parameter);
            }

            var parameterTypeMenu = m_Root.Query<ToolbarMenu>("parameter-type-menu").First();
            foreach (var parameterType in s_ParameterTypes)
            {
                parameterTypeMenu.menu.AppendAction(
                    GetParameterMetaData(parameterType).typeDisplayName,
                    a => { AddNewParameter(parameterType); },
                    a => DropdownMenuAction.Status.Normal);
            }

            var scenarioField = m_ScenarioContainer.Query<PropertyField>("scenario-field").First();
            scenarioField.RegisterCallback<ChangeEvent<Scenario>>(
                evt => evt.newValue.parameterConfiguration = m_Config);

            return m_Root;
        }

        void AddNewParameter(Type parameterType)
        {
            var parameterComponent = (Parameter)m_Config.gameObject.AddComponent(parameterType);
            parameterComponent.hideFlags = HideFlags.HideInInspector;
            m_Config.parameters.Add(parameterComponent);

            // var samplerTypes = GetDerivedTypeOptions(parameterComponent.SamplerType());
            // SetNewSampler(parameterComponent, samplerTypes[0]);

            AddNewParameterToContainer(parameterComponent);
        }

        void RemoveParameter(VisualElement template)
        {
            var paramIndex = m_ParameterContainer.IndexOf(template);
            m_ParameterContainer.RemoveAt(paramIndex);

            var param = m_Config.parameters[paramIndex];
            m_Config.parameters.RemoveAt(paramIndex);

            DestroyImmediate(param);
        }

        void AddNewParameterToContainer(Parameter parameterComponent)
        {
            var templateClone = m_ParameterTemplate.CloneTree();

            var so = new SerializedObject(parameterComponent);
            templateClone.Bind(so);

            var removeButton = templateClone.Query<Button>("remove-parameter").First();
            removeButton.RegisterCallback<MouseUpEvent>(evt => RemoveParameter(templateClone));

            var parameterTypeLabel = templateClone.Query<Label>("parameter-type-label").First();
            parameterTypeLabel.text = GetParameterMetaData(parameterComponent.GetType()).typeDisplayName;

            var moveUpButton = templateClone.Query<Button>("move-up-button").First();
            moveUpButton.RegisterCallback<MouseUpEvent>(evt => MoveProperty(templateClone, -1));
            var moveDownButton = templateClone.Query<Button>("move-down-button").First();
            moveDownButton.RegisterCallback<MouseUpEvent>(evt => MoveProperty(templateClone, 1));

            // var hasTargetToggle = templateClone.Query<Toggle>("has-target-toggle").First();
            // var targetContainer = templateClone.Query<VisualElement>("target-container").First();
            // hasTargetToggle.RegisterCallback<ChangeEvent<bool>>(
            //     evt => ToggleTargetContainer(targetContainer, parameterComponent.hasTarget));
            // ToggleTargetContainer(targetContainer, parameterComponent.hasTarget);

            // var targetField = templateClone.Query<PropertyField>("target-field").First();
            // var propertyMenu = templateClone.Query<ToolbarMenu>("property-select-menu").First();
            // targetField.RegisterCallback<ChangeEvent<Object>>(
            //     evt =>
            //     {
            //         propertyMenu.text = "";
            //         parameterComponent.propertyTarget = null;
            //         AppendActionsToPropertySelectMenu(parameterComponent, propertyMenu);
            //     });
            // AppendActionsToPropertySelectMenu(parameterComponent, propertyMenu);

            var samplersContainer = templateClone.Query<VisualElement>("samplers-container").First();
            CreatePropertyFields(samplersContainer, so);

            // var samplerTypeDropDown = templateClone.Query<ToolbarMenu>("sampler-type-dropdown").First();
            // var samplerFieldsContainer = templateClone.Query<VisualElement>("sampler-fields-container").First();
            // CreateSamplerPropertyFields(samplerFieldsContainer, parameterComponent.sampler);
            // samplerTypeDropDown.text = parameterComponent.sampler.GetType().Name;
            //
            // var samplerTypes = GetDerivedTypeOptions(parameterComponent.SamplerType());
            // foreach (var samplerType in samplerTypes)
            // {
            //     samplerTypeDropDown.menu.AppendAction(
            //         samplerType.Name,
            //         a =>
            //         {
            //             samplerTypeDropDown.text = samplerType.Name;
            //             SetNewSampler(parameterComponent, samplerType);
            //             CreateSamplerPropertyFields(samplerFieldsContainer, parameterComponent.sampler);
            //         },
            //         a => DropdownMenuAction.Status.Normal);
            // }

            m_ParameterContainer.Add(templateClone);
        }

        static void ToggleTargetContainer(VisualElement targetContainer, bool toggle)
        {
            targetContainer.style.display = toggle
                ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        void SwapParameters(int first, int second)
        {
            var firstElement = m_ParameterContainer[first];
            var secondElement = m_ParameterContainer[second];
            m_ParameterContainer.RemoveAt(second);
            m_ParameterContainer.RemoveAt(first);
            m_ParameterContainer.Insert(first, secondElement);
            m_ParameterContainer.Insert(second, firstElement);

            var firstParameter = m_Config.parameters[first];
            var secondParameter = m_Config.parameters[second];
            m_Config.parameters[first] = secondParameter;
            m_Config.parameters[second] = firstParameter;
        }

        void MoveProperty(VisualElement template, int direction)
        {
            var paramIndex = m_ParameterContainer.IndexOf(template);
            if (direction == -1 && paramIndex > 0)
            {
                SwapParameters(paramIndex - 1, paramIndex);
            }
            else if (direction == 1 && paramIndex < m_Config.parameters.Count - 1)
            {
                SwapParameters(paramIndex, paramIndex + 1);
            }
        }

        // static List<PropertyTarget> GatherPropertyOptions(GameObject obj, Type propertyType)
        // {
        //     var options = new List<PropertyTarget>();
        //     foreach (var component in obj.GetComponents<Component>())
        //     {
        //         if (component == null)
        //             continue;
        //
        //         var componentType = component.GetType();
        //
        //         var fieldInfos = componentType.GetFields();
        //         foreach (var fieldInfo in fieldInfos)
        //         {
        //             if (fieldInfo.FieldType == propertyType)
        //                 options.Add(new PropertyTarget()
        //                 {
        //                     gameObject = obj,
        //                     component = component,
        //                     propertyName = fieldInfo.Name,
        //                     fieldOrProperty = FieldOrProperty.Field
        //                 });
        //         }
        //
        //         var propertyInfos = componentType.GetProperties();
        //         foreach (var propertyInfo in propertyInfos)
        //         {
        //             if (propertyInfo.PropertyType == propertyType)
        //                 options.Add(new PropertyTarget()
        //                 {
        //                     gameObject = obj,
        //                     component = component,
        //                     propertyName = propertyInfo.Name,
        //                     fieldOrProperty = FieldOrProperty.Property
        //                 });
        //         }
        //     }
        //
        //     return options;
        // }

        // static void AppendActionsToPropertySelectMenu(Parameter parameterComponent, ToolbarMenu propertyMenu)
        // {
        //     propertyMenu.menu.MenuItems().Clear();
        //     if (parameterComponent.target == null)
        //         return;
        //
        //     var options = GatherPropertyOptions(parameterComponent.target, parameterComponent.SampleType());
        //     foreach (var option in options)
        //     {
        //         propertyMenu.menu.AppendAction(
        //             option.propertyName,
        //             a =>
        //             {
        //                 parameterComponent.propertyTarget = option;
        //                 propertyMenu.text = option.propertyName;
        //             },
        //             a => DropdownMenuAction.Status.Normal);
        //     }
        //
        //     if (parameterComponent.propertyTarget != null)
        //     {
        //         propertyMenu.text = parameterComponent.propertyTarget.propertyName;
        //     }
        // }

        // void SetNewSampler(Parameter parameterComponent, Type samplerType)
        // {
        //     if (parameterComponent.sampler != null)
        //         DestroyImmediate(parameterComponent.sampler);
        //
        //     var newSampler = (SamplerBase)m_Config.gameObject.AddComponent(samplerType);
        //     newSampler.parameter = parameterComponent;
        //     newSampler.hideFlags = HideFlags.HideInInspector;
        //     parameterComponent.sampler = newSampler;
        // }

        void CreatePropertyFields(VisualElement fieldsContainer, SerializedObject so)
        {
            fieldsContainer.Clear();
            var iterator = so.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.propertyPath == "m_Script" || iterator.propertyPath == "parameterName")
                        continue;
                    if (iterator.type == "PPtr<$Sampler>")
                        CreateSamplerUI(fieldsContainer, so, iterator.Copy());
                    else if (iterator.type == "AdrFloat")
                    {
                        var adrFloatTemplate = m_AdrFloatTemplate.CloneTree();
                        var field = iterator.Copy();
                        adrFloatTemplate.BindProperty(field);
                        var seedField = adrFloatTemplate.Query<IntegerField>("seed").First();
                        seedField.RegisterValueChangedCallback((e) =>
                        {
                            if (e.newValue <= 0)
                            {
                                seedField.value = 0;
                                field.FindPropertyRelative("baseRandomSeed").intValue = 0;
                                so.ApplyModifiedProperties();
                                e.StopImmediatePropagation();
                            }
                        });
                        fieldsContainer.Add(adrFloatTemplate);
                    }
                    else
                    {
                        var propertyField = new PropertyField(iterator.Copy());
                        propertyField.Bind(so);
                        fieldsContainer.Add(propertyField);
                    }
                } while (iterator.NextVisible(false));
            }
        }

        static string UppercaseFirstLetter(string s)
        {
            return string.IsNullOrEmpty(s) ? string.Empty : char.ToUpper(s[0]) + s.Substring(1);
        }

        void CreateSamplerUI(VisualElement container, SerializedObject parameterSerializedObject, SerializedProperty field)
        {
            var sampler = (Sampler)field.objectReferenceValue;
            if (sampler == null)
            {
                sampler = m_Config.gameObject.AddComponent<UniformSampler>();
                ((UniformSampler)sampler).adrFloat.baseRandomSeed = (uint)Random.Range(1, int.MaxValue);
                sampler.hideFlags = HideFlags.HideInInspector;
                field.objectReferenceValue = sampler;
                parameterSerializedObject.ApplyModifiedProperties();
            }
            var so = new SerializedObject(sampler);
            var template = m_SamplerTemplate.CloneTree();

            var samplerName = template.Query<Label>("sampler-name").First();
            samplerName.text = UppercaseFirstLetter(field.propertyPath);

            var propertiesContainer = template.Query<VisualElement>("fields-container").First();
            var samplerTypeDropDown = template.Query<ToolbarMenu>("sampler-type-dropdown").First();
            samplerTypeDropDown.text = GetSamplerMetaData(sampler.GetType()).displayName;

            foreach (var samplerType in s_SamplerTypes)
            {
                var displayName = GetSamplerMetaData(samplerType).displayName;
                samplerTypeDropDown.menu.AppendAction(
                    displayName,
                    a =>
                    {
                        var newSampler = m_Config.gameObject.AddComponent(samplerType);
                        if (samplerType.IsSubclassOf(typeof(RandomSampler)))
                            ((RandomSampler)newSampler).adrFloat.baseRandomSeed = (uint)Random.Range(1, int.MaxValue);

                        newSampler.hideFlags = HideFlags.HideInInspector;
                        field.objectReferenceValue = newSampler;
                        so.Dispose();
                        DestroyImmediate(sampler);
                        parameterSerializedObject.ApplyModifiedProperties();

                        samplerTypeDropDown.text = displayName;
                        so = new SerializedObject(newSampler);
                        CreatePropertyFields(propertiesContainer, so);
                    },
                    a => DropdownMenuAction.Status.Normal);
            }

            CreatePropertyFields(propertiesContainer, so);
            container.Add(template);
        }

        static ParameterMetaData GetParameterMetaData(Type type)
        {
            return (ParameterMetaData)Attribute.GetCustomAttribute(type, typeof(ParameterMetaData));
        }

        static SamplerMetaData GetSamplerMetaData(Type type)
        {
            return (SamplerMetaData)Attribute.GetCustomAttribute(type, typeof(SamplerMetaData));
        }

        static void GatherParameterAndSamplerTypes()
        {
            var paramAssembly = typeof(Parameter).Assembly;
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assemblies = new List<Assembly> { paramAssembly };
            foreach (var assembly in allAssemblies)
            {
                foreach (var asm in assembly.GetReferencedAssemblies())
                {
                    if (asm.FullName == paramAssembly.GetName().FullName)
                    {
                        assemblies.Add(assembly);
                        break;
                    }
                }
            }

            var parameterTypes = new List<Type>();
            var samplerTypes = new List<Type>();
            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        var isNotAbstract = (type.Attributes & TypeAttributes.Abstract) == 0;
                        if (typeof(Parameter).IsAssignableFrom(type) && isNotAbstract && GetParameterMetaData(type) != null)
                            parameterTypes.Add(type);
                        else if (typeof(Sampler).IsAssignableFrom(type) && isNotAbstract && GetSamplerMetaData(type) != null)
                            samplerTypes.Add(type);
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    Debug.LogWarning("Exception Happened! ");
                    // TODO: figure out why we get this exception
                }
            }
            s_ParameterTypes = parameterTypes.ToArray();
            s_SamplerTypes = samplerTypes.ToArray();
        }
    }
}
