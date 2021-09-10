﻿using System;
using System.Collections;
using UnityEditor;
using UnityEditor.Perception.Randomization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Randomization.VisualElements.AssetSource
{
    class AssetListElement : VisualElement
    {
        SerializedProperty m_Property;
        IList list => (IList)StaticData.GetManagedReferenceValue(m_Property);

        public AssetListElement(SerializedProperty property, Type itemType)
        {
            m_Property = property;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/AssetSource/AssetListElement.uxml");
            template.CloneTree(this);

            var listView = this.Q<ListView>("assets");
            listView.itemsSource = list;
            listView.itemHeight = 22;
            listView.selectionType = SelectionType.None;
            listView.style.flexGrow = 1.0f;
            listView.style.height = new StyleLength(listView.itemHeight * 4);
            listView.makeItem = () =>
            {
                return new AssetListItemElement(m_Property, itemType);
            };
            listView.bindItem = (element, i) =>
            {
                var optionElement = (AssetListItemElement)element;
                optionElement.BindProperties(i);
                var removeButton = optionElement.Q<Button>("remove");
                removeButton.clicked += () =>
                {
                    // First delete sets option to null, second delete removes option
                    var numOptions = m_Property.arraySize;
                    m_Property.DeleteArrayElementAtIndex(i);
                    if (numOptions == m_Property.arraySize)
                        m_Property.DeleteArrayElementAtIndex(i);

                    m_Property.serializedObject.ApplyModifiedProperties();
                    listView.itemsSource = list;
                    listView.Refresh();
                };
            };

            var addOptionButton = this.Q<Button>("add-asset");
            addOptionButton.clicked += () =>
            {
                m_Property.arraySize++;
                m_Property.serializedObject.ApplyModifiedProperties();
                listView.itemsSource = list;
                listView.Refresh();
                listView.ScrollToItem(m_Property.arraySize);
            };

            var addFolderButton = this.Q<Button>("add-folder");
            addFolderButton.clicked += () =>
            {
                var folderPath = EditorUtility.OpenFolderPanel(
                    "Add Assets From Folder", Application.dataPath, string.Empty);
                if (folderPath == string.Empty)
                    return;

                var assets = AssetLoadingUtilities.LoadAssetsFromFolder(folderPath, itemType);
                var optionsIndex = m_Property.arraySize;
                m_Property.arraySize += assets.Count;
                for (var i = 0; i < assets.Count; i++)
                {
                    var optionProperty = m_Property.GetArrayElementAtIndex(optionsIndex + i);
                    optionProperty.objectReferenceValue = assets[i];
                }

                m_Property.serializedObject.ApplyModifiedProperties();
                listView.itemsSource = list;
                listView.Refresh();
            };

            var clearOptionsButton = this.Q<Button>("clear-assets");
            clearOptionsButton.clicked += () =>
            {
                m_Property.arraySize = 0;
                m_Property.serializedObject.ApplyModifiedProperties();
                listView.itemsSource = list;
                listView.Refresh();
            };
        }
    }
}
