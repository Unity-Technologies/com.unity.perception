using System;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization
{
    class RandomizerList : VisualElement
    {
        VisualElement m_Container;
        SerializedProperty m_Property;

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
            Undo.undoRedoPerformed += () =>
            {
                m_Property.serializedObject.Update();
                RefreshList();
            };
        }

        public ScenarioBase scenario => (ScenarioBase)m_Property.serializedObject.targetObject;

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

        void RefreshList()
        {
            m_Container.Clear();
            for (var i = 0; i < m_Property.arraySize; i++)
                m_Container.Add(new RandomizerElement(m_Property.GetArrayElementAtIndex(i), this));
        }

        public void AddRandomizer(Type randomizerType)
        {
            Undo.RegisterCompleteObjectUndo(m_Property.serializedObject.targetObject, "Add Randomizer");
            scenario.CreateRandomizer(randomizerType);
            m_Property.serializedObject.Update();
            RefreshList();
        }

        public void RemoveRandomizer(RandomizerElement element)
        {
            Undo.RegisterCompleteObjectUndo(m_Property.serializedObject.targetObject, "Remove Randomizer");
            scenario.RemoveRandomizerAt(element.parent.IndexOf(element));
            m_Property.serializedObject.Update();
            RefreshList();
        }

        public void ReorderRandomizer(int currentIndex, int nextIndex)
        {
            if (currentIndex == nextIndex)
                return;
            if (nextIndex > currentIndex)
                nextIndex--;
            Undo.RegisterCompleteObjectUndo(m_Property.serializedObject.targetObject, "Reorder Randomizer");
            var randomizer = scenario.GetRandomizer(currentIndex);
            scenario.RemoveRandomizerAt(currentIndex);
            scenario.InsertRandomizer(nextIndex, randomizer);
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
