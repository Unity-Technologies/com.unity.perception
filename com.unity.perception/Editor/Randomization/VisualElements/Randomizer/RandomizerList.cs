﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Experimental.Perception.Randomization.Editor;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;
using UnityEngine.UIElements;

namespace UnityEngine.Experimental.Perception.Randomization.VisualElements
{
    class RandomizerList : VisualElement
    {
        SerializedProperty m_Property;
        VisualElement m_Container;
        ToolbarMenu m_AddRandomizerMenu;
        public HashSet<Type> randomizerTypeSet = new HashSet<Type>();

        ScenarioBase scenario => (ScenarioBase)m_Property.serializedObject.targetObject;

        VisualElement inspectorContainer
        {
            get
            {
                var viewport = parent;
                while (!viewport.ClassListContains("unity-inspector-main-container"))
                    viewport = viewport.parent;
                return viewport;
            }
        }

        public RandomizerList(SerializedProperty property)
        {
            m_Property = property;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/Randomizer/RandomizerList.uxml").CloneTree(this);

            m_Container = this.Q<VisualElement>("randomizers-container");

            var addRandomizerButton = this.Q<Button>("add-randomizer-button");
            addRandomizerButton.clicked += () =>
            {
                inspectorContainer.Add(new AddRandomizerMenu(inspectorContainer, addRandomizerButton, this));
            };

            var expandAllButton = this.Q<Button>("expand-all");
            expandAllButton.clicked += () => CollapseRandomizers(false);

            var collapseAllButton = this.Q<Button>("collapse-all");
            collapseAllButton.clicked += () => CollapseRandomizers(true);

            RefreshList();
        }

        void RefreshList()
        {
            m_Container.Clear();
            for (var i = 0; i < m_Property.arraySize; i++)
                m_Container.Add(new RandomizerElement(m_Property.GetArrayElementAtIndex(i), this));
            randomizerTypeSet.Clear();
            foreach (var randomizer in scenario.randomizers)
                randomizerTypeSet.Add(randomizer.GetType());
        }

        public void AddRandomizer(Type randomizerType)
        {
            var newRandomizer = scenario.CreateRandomizer(randomizerType);
            newRandomizer.RandomizeParameterSeeds();
            m_Property.serializedObject.Update();
            RefreshList();
        }

        public void RemoveRandomizer(RandomizerElement element)
        {
            scenario.RemoveRandomizer(element.randomizerType);
            m_Property.serializedObject.Update();
            RefreshList();
        }

        public void ReorderRandomizer(int currentIndex, int nextIndex)
        {
            if (currentIndex == nextIndex)
                return;
            scenario.ReorderRandomizer(currentIndex, nextIndex);
            m_Property.serializedObject.Update();
            RefreshList();
        }

        void CollapseRandomizers(bool collapsed)
        {
            foreach (var child in m_Container.Children())
                ((RandomizerElement)child).collapsed = collapsed;
        }
    }
}
