using System;
using System.Collections.Generic;
using System.Data.OleDb;
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
        Parameter m_Parameter;
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
                    m_CollapseIcon.AddToClassList(k_FoldoutOpenClass);
                    m_CollapseIcon.RemoveFromClassList(k_FoldoutClosedClass);
                    ToggleTargetContainer(m_Properties, true);
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
            m_Parameter = parameter;
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

            m_CollapseIcon = this.Q<VisualElement>("collapse");
            m_Properties = this.Q<VisualElement>("properties");
            m_CollapseIcon.RegisterCallback<MouseUpEvent>(evt => Collapsed = !Collapsed);

            m_ExtraProperties = this.Q<VisualElement>("extra-properties");
            CreatePropertyFields();
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

            if (m_Parameter is ICategoricalParameter)
            {
                CreateCategoricalParameterFields();
                return;
            }

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

        void CreateCategoricalParameterFields()
        {
            var categoricalParameter = (ICategoricalParameter)m_Parameter;
            var categoricalParameterTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/CategoricalParameterTemplate.uxml").CloneTree();

            var optionsProperty = m_SerializedObject.FindProperty("options");
            var probabilitiesProperty = m_SerializedObject.FindProperty("probabilities");
            var probabilities = categoricalParameter.Probabilities;

            var optionsContainer = categoricalParameterTemplate.Q<VisualElement>("options-container");

            var uniformToggle = categoricalParameterTemplate.Q<Toggle>("uniform");
            uniformToggle.BindProperty(m_SerializedObject.FindProperty("uniform"));
            void ToggleProbabilityFields(bool toggle)
            {
                if (toggle)
                    optionsContainer.AddToClassList("uniform-probability");
                else
                    optionsContainer.RemoveFromClassList("uniform-probability");
            }
            uniformToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                ToggleProbabilityFields(evt.newValue);
            });
            ToggleProbabilityFields(uniformToggle.value);

            VisualElement MakeItem() => new CategoricalOptionElement();

            var listView = new ListView()
            {
                itemsSource = probabilities,
                itemHeight = 20,
                makeItem = MakeItem,
                selectionType = SelectionType.None
            };
            listView.style.height = new StyleLength(80);
            listView.style.flexGrow = 1.0f;

            var sizeField = categoricalParameterTemplate.Q<IntegerField>("size");
            sizeField.value = categoricalParameter.OptionsCount();
            sizeField.isDelayed = true;
            sizeField.RegisterCallback<ChangeEvent<int>>(evt =>
            {
                var value = evt.newValue;
                if (evt.newValue < 0)
                {
                    evt.StopImmediatePropagation();
                    value = 0;
                }
                categoricalParameter.Resize(value);
                m_SerializedObject.Update();
                listView.Refresh();
            });

            void BindItem(VisualElement e, int i)
            {
                var option = optionsProperty.GetArrayElementAtIndex(i);
                var probability = probabilitiesProperty.GetArrayElementAtIndex(i);
                var optionElement = (CategoricalOptionElement)e;
                optionElement.BindProperties(i, option, probability);
                var removeButton = optionElement.Q<Button>("remove");
                removeButton.clicked += () =>
                {
                    categoricalParameter.RemoveAt(i);
                    m_SerializedObject.Update();
                    listView.Refresh();
                    sizeField.value = categoricalParameter.OptionsCount();
                };
            }

            listView.bindItem = BindItem;

            var scrollView = listView.Q<ScrollView>();
            listView.RegisterCallback<WheelEvent>(evt =>
            {
                if (!scrollView.showVertical)
                    return;
                if (Mathf.Approximately(scrollView.scrollOffset.y, 0f) && evt.delta.y < 0f)
                    evt.StopImmediatePropagation();
                else if (Mathf.Approximately(scrollView.scrollOffset.y, scrollView.verticalScroller.highValue) && evt.delta.y > 0f)
                    evt.StopImmediatePropagation();
            });



            optionsContainer.Add(listView);
            m_ExtraProperties.Add(categoricalParameterTemplate);
        }
    }
}
