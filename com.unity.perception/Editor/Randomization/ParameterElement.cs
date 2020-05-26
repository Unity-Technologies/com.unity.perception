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
        bool m_Filtered;
        Parameter m_Parameter;
        VisualElement m_Properties;
        VisualElement m_ExtraProperties;
        VisualElement m_TargetContainer;
        ToolbarMenu m_TargetPropertyMenu;
        SerializedObject m_SerializedObject;

        const string k_CollapsedParameterClass = "collapsed-parameter";

        public int ParameterIndex => parent.IndexOf(this);
        public ParameterConfigurationEditor ConfigEditor { get; private set; }

        public bool Collapsed
        {
            get => ClassListContains(k_CollapsedParameterClass);
            set
            {
                if (value)
                    AddToClassList(k_CollapsedParameterClass);
                else
                    RemoveFromClassList(k_CollapsedParameterClass);
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
            ConfigEditor = paramConfigEditor;
            m_Parameter = parameter;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ParameterElement.uxml");
            template.CloneTree(this);

            m_SerializedObject = new SerializedObject(parameter);
            this.Bind(m_SerializedObject);

            this.AddManipulator(new ParameterDragManipulator());

            var removeButton = this.Q<Button>("remove-parameter");
            removeButton.RegisterCallback<MouseUpEvent>(evt => paramConfigEditor.RemoveParameter(this));

            var parameterTypeLabel = this.Query<Label>("parameter-type-label").First();
            parameterTypeLabel.text = parameter.MetaData.typeDisplayName;

            m_TargetContainer = this.Q<VisualElement>("target-container");
            ToggleTargetContainer();

            m_TargetPropertyMenu = this.Q<ToolbarMenu>("property-select-menu");
            var targetField = this.Q<PropertyField>("target-field");
            targetField.RegisterCallback<ChangeEvent<Object>>((evt) =>
            {
                ClearTarget();
                parameter.target.gameObject = (GameObject)evt.newValue;
                ToggleTargetContainer();
                FillPropertySelectMenu();
            });
            FillPropertySelectMenu();

            var collapseToggle = this.Q<VisualElement>("collapse");
            collapseToggle.RegisterCallback<MouseUpEvent>(evt => Collapsed = !Collapsed);

            m_ExtraProperties = this.Q<VisualElement>("extra-properties");
            CreatePropertyFields();
        }

        void ToggleTargetContainer()
        {
            m_TargetContainer.style.display = m_Parameter.hasTarget
                ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        void ClearTarget()
        {
            m_SerializedObject.FindProperty("target.component").objectReferenceValue = null;
            m_SerializedObject.FindProperty("target.propertyName").stringValue = string.Empty;
            m_SerializedObject.ApplyModifiedProperties();
        }

        void SetTarget(ParameterTarget newTarget)
        {
            m_SerializedObject.FindProperty("target.gameObject").objectReferenceValue = newTarget.gameObject;
            m_SerializedObject.FindProperty("target.component").objectReferenceValue = newTarget.component;
            m_SerializedObject.FindProperty("target.propertyName").stringValue = newTarget.propertyName;
            m_SerializedObject.FindProperty("target.fieldOrProperty").enumValueIndex = (int)newTarget.fieldOrProperty;
            m_SerializedObject.ApplyModifiedProperties();
            m_TargetPropertyMenu.text = TargetPropertyDisplayText(m_Parameter.target);
        }

        static string TargetPropertyDisplayText(ParameterTarget target)
        {
            return $"{target.component.GetType().Name}.{target.propertyName}";
        }

        void FillPropertySelectMenu()
        {
            if (!m_Parameter.hasTarget)
                return;

            m_TargetPropertyMenu.menu.MenuItems().Clear();
            m_TargetPropertyMenu.text = m_Parameter.target.propertyName == string.Empty
                ? "Select a property"
                : TargetPropertyDisplayText(m_Parameter.target);

            var options = GatherPropertyOptions(m_Parameter.target.gameObject, m_Parameter.OutputType);
            foreach (var option in options)
            {
                m_TargetPropertyMenu.menu.AppendAction(
                    TargetPropertyDisplayText(option),
                    a => { SetTarget(option); },
                    a => DropdownMenuAction.Status.Normal);
            }
        }

        static List<ParameterTarget> GatherPropertyOptions(GameObject obj, Type propertyType)
        {
            var options = new List<ParameterTarget>();
            foreach (var component in obj.GetComponents<Component>())
            {
                if (component == null)
                    continue;
                var componentType = component.GetType();
                var fieldInfos = componentType.GetFields();
                foreach (var fieldInfo in fieldInfos)
                {
                    if (fieldInfo.FieldType == propertyType)
                        options.Add(new ParameterTarget()
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
                        options.Add(new ParameterTarget()
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
                    if (iterator.type.Contains("managedReference") &&
                        iterator.managedReferenceFieldTypename == StaticData.SamplerSerializedFieldType)
                        m_ExtraProperties.Add(new SamplerElement(iterator.Copy()));
                    else
                    {
                        var propertyField = new PropertyField(iterator.Copy());
                        propertyField.Bind(m_SerializedObject);
                        m_ExtraProperties.Add(propertyField);
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

            var listView = categoricalParameterTemplate.Q<ListView>("options");
            listView.itemsSource = probabilities;
            listView.itemHeight = 22;
            listView.selectionType = SelectionType.None;
            listView.style.flexGrow = 1.0f;
            listView.style.height = new StyleLength(listView.itemHeight * 4);

            VisualElement MakeItem() => new CategoricalOptionElement(optionsProperty, probabilitiesProperty, listView);
            listView.makeItem = MakeItem;

            void BindItem(VisualElement e, int i)
            {
                var optionElement = (CategoricalOptionElement)e;
                optionElement.BindProperties(i);
            }
            listView.bindItem = BindItem;

            var scrollView = listView.Q<ScrollView>();
            listView.RegisterCallback<WheelEvent>(evt =>
            {
                if (Mathf.Approximately(scrollView.verticalScroller.highValue, 0f))
                    return;
                if ((scrollView.scrollOffset.y <= 0f && evt.delta.y < 0f) ||
                    scrollView.scrollOffset.y >= scrollView.verticalScroller.highValue && evt.delta.y > 0f)
                    evt.StopImmediatePropagation();
            });

            var addOptionButton = categoricalParameterTemplate.Q<Button>("add-option");
            addOptionButton.clicked += () =>
            {
                optionsProperty.arraySize++;
                probabilitiesProperty.arraySize++;
                m_SerializedObject.ApplyModifiedProperties();
                listView.Refresh();
                listView.ScrollToItem(probabilitiesProperty.arraySize);
            };

            var uniformToggle = categoricalParameterTemplate.Q<Toggle>("uniform");
            uniformToggle.BindProperty(m_SerializedObject.FindProperty("uniform"));
            void ToggleProbabilityFields(bool toggle)
            {
                if (toggle)
                    listView.AddToClassList("uniform-probability");
                else
                    listView.RemoveFromClassList("uniform-probability");
            }
            uniformToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                ToggleProbabilityFields(evt.newValue);
            });
            ToggleProbabilityFields(uniformToggle.value);

            m_ExtraProperties.Add(categoricalParameterTemplate);
        }
    }
}
