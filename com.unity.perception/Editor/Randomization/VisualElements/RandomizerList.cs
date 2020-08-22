using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Experimental.Perception.Randomization.Editor;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;
using UnityEngine.UIElements;

namespace UnityEngine.Experimental.Perception.Randomization.VisualElements
{
    class RandomizerList : VisualElement
    {
        SerializedProperty m_Property;
        VisualElement m_Container;
        ToolbarMenu m_AddRandomizerMenu;

        ScenarioBase scenario => (ScenarioBase)m_Property.serializedObject.targetObject;

        public RandomizerList(SerializedProperty property)
        {
            m_Property = property;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/RandomizerList.uxml").CloneTree(this);

            m_Container = this.Q<VisualElement>("randomizers-container");
            m_AddRandomizerMenu = this.Q<ToolbarMenu>("add-randomizer");

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
            SetMenuOptions();
        }

        void SetMenuOptions()
        {
            m_AddRandomizerMenu.menu.MenuItems().Clear();
            var typeSet = new HashSet<Type>();
            foreach (var randomizer in scenario.randomizers)
                typeSet.Add(randomizer.GetType());
            foreach (var randomizerType in StaticData.randomizerTypes)
            {
                if (typeSet.Contains(randomizerType))
                    continue;
                m_AddRandomizerMenu.menu.AppendAction(
                    Parameter.GetDisplayName(randomizerType),
                    a => { AddRandomizer(randomizerType); });
            }
        }

        void AddRandomizer(Type randomizerType)
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
