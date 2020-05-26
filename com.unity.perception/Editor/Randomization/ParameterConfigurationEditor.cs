using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Perception.Randomization.Parameters.Abstractions;
using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine.Perception.Randomization.Parameters.MonoBehaviours;
using UnityEngine.Perception.Randomization.Scenarios.Abstractions;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEngine.Perception.Randomization.Samplers.Editor
{
    [CustomEditor(typeof(ParameterConfiguration))]
    public class ParameterConfigurationEditor : UnityEditor.Editor
    {
        ParameterConfiguration m_Config;

        VisualElement m_Root;
        VisualElement m_ParameterContainer;
        VisualElement m_ScenarioContainer;
        VisualTreeAsset m_ParameterTemplate;

        const string k_TemplatesFolder = "Packages/com.unity.perception/Editor/Randomization/Uxml";

        public void OnEnable()
        {
            m_Config = (ParameterConfiguration)target;
            EditorApplication.update += UpdateStatsContainer;
        }

        public void OnDisable()
        {
            // ReSharper disable once DelegateSubtraction
            EditorApplication.update -= UpdateStatsContainer;
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{k_TemplatesFolder}/ParameterConfiguration.uxml").Instantiate();

            m_ParameterContainer = m_Root.Query<VisualElement>("parameter-container").First();
            m_ScenarioContainer = m_Root.Query<VisualElement>("configuration-container");

            m_ParameterTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{k_TemplatesFolder}/ParameterTemplate.uxml");
            foreach (var parameter in m_Config.parameters)
            {
                AddNewParameterToContainer(parameter);
            }

            var parameterTypeMenu = m_Root.Query<ToolbarMenu>("parameter-type-menu").First();
            var parameterTypes = GetDerivedTypeOptions(typeof(ParameterBase));
            foreach (var parameterType in parameterTypes)
            {
                parameterTypeMenu.menu.AppendAction(
                    parameterType.Name,
                    a => { AddNewParameter(parameterType); },
                    a => DropdownMenuAction.Status.Normal);
            }

            var scenarioField = m_ScenarioContainer.Query<PropertyField>("scenario-field").First();
            scenarioField.RegisterCallback<ChangeEvent<Scenario>>(
                evt => evt.newValue.parameterConfiguration = m_Config);

            UpdateStatsContainer();
            return m_Root;
        }

        void AddNewParameter(Type parameterType)
        {
            var parameterComponent = (ParameterBase)m_Config.gameObject.AddComponent(parameterType);
            parameterComponent.hideFlags = HideFlags.HideInInspector;
            m_Config.parameters.Add(parameterComponent);

            var samplerTypes = GetDerivedTypeOptions(parameterComponent.SamplerType());
            SetNewSampler(parameterComponent, samplerTypes[0]);

            AddNewParameterToContainer(parameterComponent);
        }

        void RemoveParameter(VisualElement template)
        {
            var paramIndex = m_ParameterContainer.IndexOf(template);
            m_ParameterContainer.RemoveAt(paramIndex);

            var param = m_Config.parameters[paramIndex];
            m_Config.parameters.RemoveAt(paramIndex);
            DestroyImmediate(param.sampler);
            DestroyImmediate(param);
        }

        void AddNewParameterToContainer(ParameterBase parameterComponent)
        {
            var templateClone = m_ParameterTemplate.CloneTree();

            var so = new SerializedObject(parameterComponent);
            templateClone.Bind(so);

            var removeButton = templateClone.Query<Button>("remove-parameter").First();
            removeButton.RegisterCallback<MouseUpEvent>(evt => RemoveParameter(templateClone));

            var parameterTypeLabel = templateClone.Query<Label>("parameter-type-label").First();
            parameterTypeLabel.text = parameterComponent.ParameterTypeName;

            var moveUpButton = templateClone.Query<Button>("move-up-button").First();
            moveUpButton.RegisterCallback<MouseUpEvent>(evt => MoveProperty(templateClone, -1));
            var moveDownButton = templateClone.Query<Button>("move-down-button").First();
            moveDownButton.RegisterCallback<MouseUpEvent>(evt => MoveProperty(templateClone, 1));

            var hasTargetToggle = templateClone.Query<Toggle>("has-target-toggle").First();
            var targetContainer = templateClone.Query<VisualElement>("target-container").First();
            hasTargetToggle.RegisterCallback<ChangeEvent<bool>>(
                evt => ToggleTargetContainer(targetContainer, parameterComponent.hasTarget));
            ToggleTargetContainer(targetContainer, parameterComponent.hasTarget);

            var targetField = templateClone.Query<PropertyField>("target-field").First();
            var propertyMenu = templateClone.Query<ToolbarMenu>("property-select-menu").First();
            targetField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(
                evt =>
                {
                    propertyMenu.text = "";
                    parameterComponent.propertyTarget = null;
                    AppendActionsToPropertySelectMenu(parameterComponent, propertyMenu);
                });
            AppendActionsToPropertySelectMenu(parameterComponent, propertyMenu);

            var samplerTypeDropDown = templateClone.Query<ToolbarMenu>("sampler-type-dropdown").First();
            var samplerFieldsContainer = templateClone.Query<VisualElement>("sampler-fields-container").First();
            CreateSamplerPropertyFields(samplerFieldsContainer, parameterComponent.sampler);
            samplerTypeDropDown.text = parameterComponent.sampler.GetType().Name;

            var samplerTypes = GetDerivedTypeOptions(parameterComponent.SamplerType());
            foreach (var samplerType in samplerTypes)
            {
                samplerTypeDropDown.menu.AppendAction(
                    samplerType.Name,
                    a =>
                    {
                        samplerTypeDropDown.text = samplerType.Name;
                        SetNewSampler(parameterComponent, samplerType);
                        CreateSamplerPropertyFields(samplerFieldsContainer, parameterComponent.sampler);
                    },
                    a => DropdownMenuAction.Status.Normal);
            }

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

        static List<PropertyTarget> GatherPropertyOptions(GameObject obj, Type propertyType)
        {
            var options = new List<PropertyTarget>();
            foreach (var component in obj.GetComponents<Component>())
            {
                if (component == null)
                    continue;

                var componentType = component.GetType();

                var fieldInfos = componentType.GetFields();
                foreach (var fieldInfo in fieldInfos)
                {
                    if (fieldInfo.FieldType == propertyType)
                        options.Add(new PropertyTarget()
                        {
                            targetComponent = component,
                            propertyName = fieldInfo.Name,
                            targetKind = TargetKind.Field
                        });
                }

                var propertyInfos = componentType.GetProperties();
                foreach (var propertyInfo in propertyInfos)
                {
                    if (propertyInfo.PropertyType == propertyType)
                        options.Add(new PropertyTarget()
                        {
                            targetComponent = component,
                            propertyName = propertyInfo.Name,
                            targetKind = TargetKind.Property
                        });
                }
            }

            return options;
        }

        void AppendActionsToPropertySelectMenu(ParameterBase parameterComponent, ToolbarMenu propertyMenu)
        {
            propertyMenu.menu.MenuItems().Clear();
            if (parameterComponent.target == null) return;
            var options = GatherPropertyOptions(parameterComponent.target, parameterComponent.SampleType());
            foreach (var option in options)
            {
                propertyMenu.menu.AppendAction(
                    option.propertyName,
                    a =>
                    {
                        parameterComponent.propertyTarget = option;
                        propertyMenu.text = option.propertyName;
                    },
                    a => DropdownMenuAction.Status.Normal);
            }

            if (parameterComponent.propertyTarget != null)
            {
                propertyMenu.text = parameterComponent.propertyTarget.propertyName;
            }
        }

        void SetNewSampler(ParameterBase parameterComponent, Type samplerType)
        {
            if (parameterComponent.sampler != null)
                DestroyImmediate(parameterComponent.sampler);

            var newSampler = (SamplerBase)m_Config.gameObject.AddComponent(samplerType);
            newSampler.parameter = parameterComponent;
            newSampler.hideFlags = HideFlags.HideInInspector;
            parameterComponent.sampler = (SamplerBase)newSampler;
        }

        void UpdateStatsContainer()
        {
            if (m_ScenarioContainer == null)
                return;
            var totalIterationCountLabel = m_ScenarioContainer.Query<Label>("total-iteration-count-label").First();
            totalIterationCountLabel.text = $"{m_Config.TotalIterationCount}";
            var totalFrameCountLabel = m_ScenarioContainer.Query<Label>("total-frame-count-label").First();
            totalFrameCountLabel.text = $"{m_Config.TotalFrameCount}";
        }

        void CreateSamplerPropertyFields(VisualElement samplerFieldsContainer, SamplerBase sampler)
        {
            samplerFieldsContainer.Clear();
            var serializedSampler = new SerializedObject(sampler);
            var iterator = serializedSampler.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.propertyPath == "m_Script")
                        continue;
                    var propertyField = new PropertyField(iterator.Copy());
                    propertyField.Bind(serializedSampler);
                    samplerFieldsContainer.Add(propertyField);
                } while (iterator.NextVisible(false));
            }
        }

        static Type[] GetDerivedTypeOptions(Type derivedType)
        {
            var samplerTypes = (
                from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                from assemblyType in domainAssembly.GetTypes()
                where (derivedType.IsAssignableFrom(assemblyType) &&
                    (assemblyType.Attributes & TypeAttributes.Abstract) == 0)
                select assemblyType).ToArray();
            return samplerTypes;
        }
    }
}
