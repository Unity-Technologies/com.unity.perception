using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Perception.Randomization
{
    class ParameterElement : VisualElement
    {
        VisualElement m_PropertiesContainer;
        SerializedProperty m_SerializedProperty;

        public ParameterElement(SerializedProperty property)
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/Parameter/ParameterElement.uxml");
            template.CloneTree(this);
            m_SerializedProperty = property;
            m_PropertiesContainer = this.Q<VisualElement>("properties");
            CreatePropertyFields();
        }

        Parameter parameter => (Parameter)StaticData.GetManagedReferenceValue(m_SerializedProperty);
        CategoricalParameterBase categoricalParameter => (CategoricalParameterBase)parameter;

        void CreatePropertyFields()
        {
            m_PropertiesContainer.Clear();

            if (parameter is CategoricalParameterBase)
            {
                CreateCategoricalParameterFields();
                return;
            }

            var nextSiblingProperty = m_SerializedProperty.Copy();
            nextSiblingProperty.NextVisible(false);

            var currentProperty = m_SerializedProperty.Copy();
            if (currentProperty.NextVisible(true))
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;
                    var propertyField = new PropertyField(currentProperty.Copy());
                    propertyField.Bind(currentProperty.serializedObject);
                    m_PropertiesContainer.Add(propertyField);
                } while (currentProperty.NextVisible(false));
        }

        void CreateCategoricalParameterFields()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/Parameter/CategoricalParameterTemplate.uxml").CloneTree();

            var optionsProperty = m_SerializedProperty.FindPropertyRelative("m_Categories");
            var probabilitiesProperty = m_SerializedProperty.FindPropertyRelative("probabilities");
            var probabilities = categoricalParameter.probabilities;

            var listView = template.Q<ListView>("options");
            listView.itemsSource = probabilities;
            listView.itemHeight = 22;
            listView.selectionType = SelectionType.None;
            listView.style.flexGrow = 1.0f;
            listView.style.height = new StyleLength(listView.itemHeight * 4);

            var uniformToggle = template.Q<Toggle>("uniform");

            VisualElement MakeItem()
            {
                return new CategoricalOptionElement(
                    optionsProperty, probabilitiesProperty);
            }

            listView.makeItem = MakeItem;

            void BindItem(VisualElement e, int i)
            {
                var optionElement = (CategoricalOptionElement)e;
                optionElement.BindProperties(i);
                var removeButton = optionElement.Q<Button>("remove");
                removeButton.clicked += () =>
                {
                    probabilitiesProperty.DeleteArrayElementAtIndex(i);

                    // First delete sets option to null, second delete removes option
                    var numOptions = optionsProperty.arraySize;
                    optionsProperty.DeleteArrayElementAtIndex(i);

                    if (numOptions == optionsProperty.arraySize)
                    {
                        optionsProperty.DeleteArrayElementAtIndex(i);
                    }

                    m_SerializedProperty.serializedObject.ApplyModifiedProperties();

                    listView.itemsSource = categoricalParameter.probabilities;
                    listView.Refresh();
                };
            }

            void ResetProbabilities()
            {
                var uniformProbability = probabilitiesProperty.arraySize > 0 ? 1f / probabilitiesProperty.arraySize : 0;
                for (var i = 0; i < probabilitiesProperty.arraySize; i++)
                {
                    probabilitiesProperty.GetArrayElementAtIndex(i).floatValue = uniformProbability;
                }
            }

            listView.bindItem = BindItem;

            var addOptionButton = template.Q<Button>("add-option");
            addOptionButton.clicked += () =>
            {
                probabilitiesProperty.arraySize++;
                optionsProperty.arraySize++;
                var newOption = optionsProperty.GetArrayElementAtIndex(optionsProperty.arraySize - 1);
                switch (newOption.propertyType)
                {
                    case SerializedPropertyType.ObjectReference:
                        newOption.objectReferenceValue = null;
                        break;
                    case SerializedPropertyType.String:
                        newOption.stringValue = string.Empty;
                        break;
                }

                // New items probability will be 0, unless uniform toggle is true
                probabilitiesProperty.GetArrayElementAtIndex(probabilitiesProperty.arraySize - 1).floatValue = 0;

                if (uniformToggle.value)
                    ResetProbabilities();

                m_SerializedProperty.serializedObject.ApplyModifiedProperties();
                listView.itemsSource = categoricalParameter.probabilities;
                listView.Refresh();
                listView.ScrollToItem(probabilitiesProperty.arraySize);
            };

            var addFolderButton = template.Q<Button>("add-folder");
            if (categoricalParameter.sampleType.IsSubclassOf(typeof(Object)))
                addFolderButton.clicked += () =>
                {
                    var folderPath = EditorUtility.OpenFolderPanel(
                        "Add Options From Folder", Application.dataPath, string.Empty);
                    if (folderPath == string.Empty)
                        return;
                    var categories = LoadAssetsFromFolder(folderPath, categoricalParameter.sampleType);

                    var optionsIndex = optionsProperty.arraySize;
                    optionsProperty.arraySize += categories.Count;

                    probabilitiesProperty.arraySize += categories.Count;

                    for (var i = 0; i < categories.Count; i++)
                    {
                        var optionProperty = optionsProperty.GetArrayElementAtIndex(optionsIndex + i);
                        optionProperty.objectReferenceValue = categories[i];
                        probabilitiesProperty.GetArrayElementAtIndex(i).floatValue = 0;
                    }

                    if (uniformToggle.value)
                        ResetProbabilities();

                    m_SerializedProperty.serializedObject.ApplyModifiedProperties();
                    listView.itemsSource = categoricalParameter.probabilities;
                    listView.Refresh();
                };
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
                if (scrollView.scrollOffset.y <= 0f && evt.delta.y < 0f ||
                    scrollView.scrollOffset.y >= scrollView.verticalScroller.highValue && evt.delta.y > 0f)
                    evt.StopImmediatePropagation();
            });


            var uniformProperty = m_SerializedProperty.FindPropertyRelative("uniform");
            uniformToggle.BindProperty(uniformProperty);

            void ToggleUniform()
            {
                if (uniformToggle.value)
                    listView.AddToClassList("collapsed");
                else
                    listView.RemoveFromClassList("collapsed");
            }
            ToggleUniform();

            if (Application.isPlaying)
                uniformToggle.SetEnabled(false);
            else
                uniformToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
                {
                    ToggleUniform();
                    if (!evt.newValue)
                        return;
                    var numOptions = optionsProperty.arraySize;
                    var uniformProbabilityValue = 1f / numOptions;
                    for (var i = 0; i < numOptions; i++)
                    {
                        var probability = probabilitiesProperty.GetArrayElementAtIndex(i);
                        probability.floatValue = uniformProbabilityValue;
                    }

                    m_SerializedProperty.serializedObject.ApplyModifiedProperties();
                    listView.itemsSource = categoricalParameter.probabilities;
                    listView.Refresh();
                });

            m_PropertiesContainer.Add(template);
        }

        static List<Object> LoadAssetsFromFolder(string folderPath, Type assetType)
        {
            if (!folderPath.StartsWith(Application.dataPath))
                throw new ApplicationException("Selected folder is not an asset folder in this project");
            var assetsPath = "Assets" + folderPath.Remove(0, Application.dataPath.Length);
            var assetIds = AssetDatabase.FindAssets($"t:{assetType.Name}", new[] { assetsPath });
            var assets = new List<Object>();
            foreach (var guid in assetIds)
                assets.Add(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), assetType));
            return assets;
        }
    }
}
