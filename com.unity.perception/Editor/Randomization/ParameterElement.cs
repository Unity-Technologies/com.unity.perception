using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    class ParameterElement : VisualElement
    {
        int m_ParameterIndex;
        bool m_Filtered;
        VisualElement m_Properties;
        VisualElement m_ExtraProperties;
        VisualElement m_TargetContainer;
        ToolbarMenu m_TargetPropertyMenu;
        SerializedProperty m_SerializedProperty;

        const string k_CollapsedParameterClass = "collapsed-parameter";

        public ParameterConfigurationEditor ConfigEditor { get; private set; }
        Parameter parameter => ConfigEditor.config.parameters[m_ParameterIndex];
        CategoricalParameterBase categoricalParameter => (CategoricalParameterBase)parameter;
        public int ParameterIndex => parent.IndexOf(this);

        public bool Collapsed
        {
            get => parameter.collapsed;
            set
            {
                parameter.collapsed = value;
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

        public ParameterElement(int index, ParameterConfigurationEditor paramConfigEditor)
        {
            ConfigEditor = paramConfigEditor;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ParameterElement.uxml");
            template.CloneTree(this);

            m_ParameterIndex = index;
            m_SerializedProperty =
                ConfigEditor.serializedObject.FindProperty("parameters").GetArrayElementAtIndex(m_ParameterIndex);

            this.AddManipulator(new ParameterDragManipulator());

            Collapsed = parameter.collapsed;

            var removeButton = this.Q<Button>("remove-parameter");
            removeButton.RegisterCallback<MouseUpEvent>(evt => paramConfigEditor.RemoveParameter(this));

            var parameterTypeLabel = this.Query<Label>("parameter-type-label").First();
            parameterTypeLabel.text = parameter.MetaData.typeDisplayName;

            var parameterNameField = this.Q<TextField>("name");
            parameterNameField.isDelayed = true;
            parameterNameField.BindProperty(m_SerializedProperty.FindPropertyRelative("name"));

            m_TargetContainer = this.Q<VisualElement>("target-container");
            ToggleTargetContainer();
            m_TargetPropertyMenu = this.Q<ToolbarMenu>("property-select-menu");

            var frequencyField = this.Q<PropertyField>("application-frequency");
            frequencyField.BindProperty(m_SerializedProperty.FindPropertyRelative("target.applicationFrequency"));

            var targetField = this.Q<PropertyField>("target");
            var targetObjectProperty = m_SerializedProperty.FindPropertyRelative("target.gameObject");
            targetField.BindProperty(targetObjectProperty);
            targetField.RegisterCallback<ChangeEvent<Object>>(evt =>
            {
                if (evt.newValue == targetObjectProperty.objectReferenceValue)
                    return;
                ClearTarget();
                targetObjectProperty.objectReferenceValue = (GameObject)evt.newValue;
                m_SerializedProperty.serializedObject.ApplyModifiedProperties();
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
            m_TargetContainer.style.display = parameter.hasTarget
                ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        void ClearTarget()
        {
            m_SerializedProperty.FindPropertyRelative("target.component").objectReferenceValue = null;
            m_SerializedProperty.FindPropertyRelative("target.propertyName").stringValue = string.Empty;
            m_SerializedProperty.serializedObject.ApplyModifiedProperties();
        }

        void SetTarget(ParameterTarget newTarget)
        {
            m_SerializedProperty.FindPropertyRelative("target.gameObject").objectReferenceValue = newTarget.gameObject;
            m_SerializedProperty.FindPropertyRelative("target.component").objectReferenceValue = newTarget.component;
            m_SerializedProperty.FindPropertyRelative("target.propertyName").stringValue = newTarget.propertyName;
            m_SerializedProperty.FindPropertyRelative("target.fieldOrProperty").enumValueIndex = (int)newTarget.fieldOrProperty;
            m_SerializedProperty.serializedObject.ApplyModifiedProperties();
            m_TargetPropertyMenu.text = TargetPropertyDisplayText(parameter.target);
        }

        static string TargetPropertyDisplayText(ParameterTarget target)
        {
            return $"{target.component.GetType().Name}.{target.propertyName}";
        }

        void FillPropertySelectMenu()
        {
            if (!parameter.hasTarget)
                return;

            m_TargetPropertyMenu.menu.MenuItems().Clear();
            m_TargetPropertyMenu.text = parameter.target.propertyName == string.Empty
                ? "Select a property"
                : TargetPropertyDisplayText(parameter.target);

            var options = GatherPropertyOptions(parameter.target.gameObject, parameter.sampleType);
            foreach (var option in options)
            {
                m_TargetPropertyMenu.menu.AppendAction(
                    TargetPropertyDisplayText(option),
                    a => { SetTarget(option); });
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

            if (parameter is CategoricalParameterBase)
            {
                CreateCategoricalParameterFields();
                return;
            }

            var currentProperty = m_SerializedProperty.Copy();
            var nextSiblingProperty = m_SerializedProperty.Copy();
            nextSiblingProperty.Next(false);

            if (currentProperty.Next(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;
                    if (currentProperty.name == "name")
                        continue;
                    if (currentProperty.type.Contains("managedReference") &&
                        currentProperty.managedReferenceFieldTypename == StaticData.samplerSerializedFieldType)
                        m_ExtraProperties.Add(new SamplerElement(currentProperty.Copy(), parameter));
                    else
                    {
                        var propertyField = new PropertyField(currentProperty.Copy());
                        propertyField.Bind(currentProperty.serializedObject);
                        m_ExtraProperties.Add(propertyField);
                    }
                } while (currentProperty.NextVisible(false));
            }
        }

        void CreateCategoricalParameterFields()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/CategoricalParameterTemplate.uxml").CloneTree();

            var optionsProperty = m_SerializedProperty.FindPropertyRelative("m_Categories");
            var probabilitiesProperty = m_SerializedProperty.FindPropertyRelative("probabilities");
            var probabilities = categoricalParameter.probabilities;

            var listView = template.Q<ListView>("options");
            listView.itemsSource = probabilities;
            listView.itemHeight = 22;
            listView.selectionType = SelectionType.None;
            listView.style.flexGrow = 1.0f;
            listView.style.height = new StyleLength(listView.itemHeight * 4);

            VisualElement MakeItem() => new CategoricalOptionElement(
                optionsProperty, probabilitiesProperty);
            listView.makeItem = MakeItem;

            void BindItem(VisualElement e, int i)
            {
                var optionElement = (CategoricalOptionElement)e;
                optionElement.BindProperties(i);
                var removeButton = optionElement.Q<Button>("remove");
                removeButton.clicked += () =>
                {
                    probabilitiesProperty.DeleteArrayElementAtIndex(i);
                    optionsProperty.DeleteArrayElementAtIndex(i);
                    m_SerializedProperty.serializedObject.ApplyModifiedProperties();
                    listView.itemsSource = categoricalParameter.probabilities;
                    listView.Refresh();
                };
            }
            listView.bindItem = BindItem;

            var addOptionButton = template.Q<Button>("add-option");
            addOptionButton.clicked += () =>
            {
                probabilitiesProperty.arraySize++;
                optionsProperty.arraySize++;
                m_SerializedProperty.serializedObject.ApplyModifiedProperties();
                listView.itemsSource = categoricalParameter.probabilities;
                listView.Refresh();
                listView.ScrollToItem(probabilitiesProperty.arraySize);
            };

            var addFolderButton = template.Q<Button>("add-folder");
            if (categoricalParameter.sampleType.IsSubclassOf(typeof(Object)))
            {
                addFolderButton.clicked += () =>
                {
                    var folderPath = EditorUtility.OpenFolderPanel("Add Options From Folder", Application.dataPath, "Goober");
                    var categories = LoadAssetsFromFolder(folderPath, categoricalParameter.sampleType);
                    probabilitiesProperty.arraySize += categories.Count;
                    optionsProperty.arraySize += categories.Count;
                    var uniformProbability = 1f / categories.Count;
                    for (var i = 0; i < categories.Count; i++)
                    {
                        var optionProperty = optionsProperty.GetArrayElementAtIndex(i);
                        var probabilityProperty = probabilitiesProperty.GetArrayElementAtIndex(i);
                        optionProperty.objectReferenceValue = categories[i];
                        probabilityProperty.floatValue = uniformProbability;
                    }
                    m_SerializedProperty.serializedObject.ApplyModifiedProperties();
                    listView.itemsSource = categoricalParameter.probabilities;
                    listView.Refresh();
                };
            }
            else
                addFolderButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

            var clearOptionsButton = template.Q<Button>("clear-options");
            clearOptionsButton.clicked += () =>
            {
                probabilitiesProperty.arraySize = 0;
                optionsProperty.arraySize = 0;
                m_SerializedProperty.serializedObject.ApplyModifiedProperties();
                listView.itemsSource = categoricalParameter.probabilities;
                listView.Refresh();
            };

            var scrollView = listView.Q<ScrollView>();
            listView.RegisterCallback<WheelEvent>(evt =>
            {
                if (Mathf.Approximately(scrollView.verticalScroller.highValue, 0f))
                    return;
                if ((scrollView.scrollOffset.y <= 0f && evt.delta.y < 0f) ||
                    scrollView.scrollOffset.y >= scrollView.verticalScroller.highValue && evt.delta.y > 0f)
                    evt.StopImmediatePropagation();
            });

            var uniformToggle = template.Q<Toggle>("uniform");
            var uniformProperty = m_SerializedProperty.FindPropertyRelative("uniform");
            uniformToggle.BindProperty(uniformProperty);
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

            var seedField = template.Q<IntegerField>("seed");
            seedField.BindProperty(m_SerializedProperty.FindPropertyRelative("m_Sampler.<baseSeed>k__BackingField"));

            m_ExtraProperties.Add(template);
        }

        static List<Object> LoadAssetsFromFolder(string folderPath, Type assetType)
        {
            if (!folderPath.StartsWith(Application.dataPath))
                throw new ApplicationException("Selected folder is not an asset folder in this project");
            var assetsPath = "Assets" + folderPath.Remove(0, Application.dataPath.Length);
            var guids = AssetDatabase.FindAssets($"t:{assetType.Name}", new []{assetsPath});
            var assets = new List<Object>();
            foreach (var guid in guids)
                assets.Add(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), assetType));
            return assets;
        }
    }
}
