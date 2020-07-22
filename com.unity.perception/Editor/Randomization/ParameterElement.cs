using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    public class ParameterElement : VisualElement
    {
        const string k_FoldoutClosedClass = "foldout-closed";
        const string k_FoldoutOpenClass = "foldout-open";

        bool m_Collapsed, m_Filtered;
        VisualElement m_Properties;
        VisualElement m_CollapseIcon;
        VisualElement m_ExtraProperties;
        SerializedObject m_SerializedObject;

        public bool Collapsed
        {
            get => m_Collapsed;
            set
            {
                m_Collapsed = value;
                if (m_Collapsed)
                {
                    m_CollapseIcon.AddToClassList(k_FoldoutClosedClass);
                    m_CollapseIcon.RemoveFromClassList(k_FoldoutOpenClass);
                    ToggleTargetContainer(m_Properties, false);
                }
                else
                {
                    m_CollapseIcon.AddToClassList(k_FoldoutClosedClass);
                    m_CollapseIcon.RemoveFromClassList(k_FoldoutOpenClass);
                    ToggleTargetContainer(m_Properties, false);
                }
            }
        }

        public bool Filtered
        {
            get => m_Filtered;
            set
            {
                m_Filtered = value;
                style.display = value
                    ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                    : new StyleEnum<DisplayStyle>(DisplayStyle.None);
            }
        }

        public ParameterElement(Parameter parameter, ParameterConfigurationEditor paramConfigEditor)
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ParameterElement.uxml");
            template.CloneTree(this);

            m_SerializedObject = new SerializedObject(parameter);
            this.Bind(m_SerializedObject);

            var removeButton = this.Q<Button>("remove-parameter");
            removeButton.RegisterCallback<MouseUpEvent>(evt => paramConfigEditor.RemoveParameter(this));

            var parameterTypeLabel = this.Query<Label>("parameter-type-label").First();
            parameterTypeLabel.text = parameter.MetaData.typeDisplayName;

            var moveUpButton = this.Q<Button>("move-up-button");
            moveUpButton.RegisterCallback<MouseUpEvent>(evt => paramConfigEditor.MoveParameter(this, -1));
            var moveDownButton = this.Q<Button>("move-down-button");
            moveDownButton.RegisterCallback<MouseUpEvent>(evt => paramConfigEditor.MoveParameter(this, 1));

            var hasTargetToggle = this.Q<Toggle>("has-target-toggle");
            var targetContainer = this.Q<VisualElement>("target-container");
            hasTargetToggle.RegisterCallback<ChangeEvent<bool>>(
                evt => ToggleTargetContainer(targetContainer, parameter.hasTarget));
            ToggleTargetContainer(targetContainer, parameter.hasTarget);

            var targetField = this.Q<PropertyField>("target-field");
            var propertyMenu = this.Q<ToolbarMenu>("property-select-menu");
            targetField.RegisterCallback<ChangeEvent<Object>>(
                evt =>
                {
                    parameter.target.gameObject = (GameObject)evt.newValue;
                    FillPropertySelectMenu(parameter, propertyMenu);
                });
            FillPropertySelectMenu(parameter, propertyMenu);

            m_ExtraProperties = this.Q<VisualElement>("extra-properties");
            CreatePropertyFields();

            m_CollapseIcon = this.Q<VisualElement>("collapse");
            m_Properties = this.Q<VisualElement>("properties");
            m_CollapseIcon.RegisterCallback<MouseUpEvent>(evt => Collapsed = !Collapsed);
        }

        static void ToggleTargetContainer(VisualElement targetContainer, bool toggle)
        {
            targetContainer.style.display = toggle
                ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        static void FillPropertySelectMenu(Parameter parameterComponent, ToolbarMenu propertyMenu)
        {
            propertyMenu.menu.MenuItems().Clear();
            if (parameterComponent.target.gameObject == null)
            {
                propertyMenu.text = "";
                return;
            }

            propertyMenu.text = parameterComponent.target.propertyName == string.Empty
                ? "Select a property"
                : parameterComponent.target.propertyName;

            var options = GatherPropertyOptions(parameterComponent.target.gameObject, parameterComponent.OutputType);
            foreach (var option in options)
            {
                propertyMenu.menu.AppendAction(
                    option.propertyName,
                    a =>
                    {
                        parameterComponent.target = option;
                        propertyMenu.text = option.propertyName;
                    },
                    a => DropdownMenuAction.Status.Normal);
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
                            gameObject = obj,
                            component = component,
                            propertyName = fieldInfo.Name,
                            fieldOrProperty = FieldOrProperty.Field
                        });
                }

                var propertyInfos = componentType.GetProperties();
                foreach (var propertyInfo in propertyInfos)
                {
                    if (propertyInfo.PropertyType == propertyType)
                        options.Add(new PropertyTarget()
                        {
                            gameObject = obj,
                            component = component,
                            propertyName = propertyInfo.Name,
                            fieldOrProperty = FieldOrProperty.Property
                        });
                }
            }
            return options;
        }

        void CreatePropertyFields()
        {
            m_ExtraProperties.Clear();
            var iterator = m_SerializedObject.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.propertyPath == "m_Script" || iterator.propertyPath == "parameterName")
                        continue;
                    switch (iterator.type)
                    {
                        case "PPtr<$Sampler>":
                            m_ExtraProperties.Add(new SamplerElement(iterator.Copy()));
                            break;
                        default:
                        {
                            var propertyField = new PropertyField(iterator.Copy());
                            propertyField.Bind(m_SerializedObject);
                            m_ExtraProperties.Add(propertyField);
                            break;
                        }
                    }
                } while (iterator.NextVisible(false));
            }
        }
    }
}
