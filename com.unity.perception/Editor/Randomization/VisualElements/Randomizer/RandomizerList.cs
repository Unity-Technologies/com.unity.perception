using System;
using System.Linq;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization
{
    class RandomizerList : VisualElement
    {
        VisualElement m_Container;
        VisualElement m_OptionsContainer;
        SerializedProperty m_Property;

        public RandomizerList(SerializedProperty property)
        {
            m_Property = property;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/Randomizer/RandomizerList.uxml").CloneTree(this);

            m_Container = this.Q<VisualElement>("randomizers-container");
            m_OptionsContainer = this.Q<VisualElement>("randomizer-options-container");

            var addRandomizerButton = this.Q<Button>("add-randomizer-button");
            addRandomizerButton.clicked += () =>
            {
                inspectorContainer.Add(new AddRandomizerMenu(inspectorContainer, addRandomizerButton, this));
            };

            var expandAllButton = this.Q<Button>("expand-all");
            expandAllButton.clicked += () => SetRandomizersCollapsedState(false);

            var collapseAllButton = this.Q<Button>("collapse-all");
            collapseAllButton.clicked += () => SetRandomizersCollapsedState(true);

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
            m_OptionsContainer.SetEnabled(true);

#if UNITY_2021_3_OR_NEWER
            var missingTypes = SerializationUtility.GetManagedReferencesWithMissingTypes(scenario);
            if (missingTypes.Length > 0)
            {
                var warning = $"{missingTypes.Aggregate("", (s, mt) => $"{s}, {mt.className}")}".Substring(2);
                var errorContainer = new VisualElement();
                errorContainer.AddToClassList("scenario__info-box");
                errorContainer.AddToClassList("scenario__error-box");
                var textElement = new TextElement()
                {
                    text = $"The following randomizers were not found: {warning}. You can add the missing types " +
                        $"back in or remove all missing randomizers using the option below."
                };
                var btn = new Button(ClearNullRandomizers)
                {
                    text = $"Remove {missingTypes.Length} Missing Randomizer(s)",
                    style =
                    {
                        marginTop = 8,
                        alignSelf = Align.Center
                    }
                };
                errorContainer.Add(textElement);
                errorContainer.Add(btn);
                m_Container.Add(errorContainer);
                m_OptionsContainer.SetEnabled(false);
                return;
            }
            ;
#endif

            if (m_Property.arraySize > 0 &&
                string.IsNullOrEmpty(m_Property.GetArrayElementAtIndex(0).managedReferenceFullTypename))
            {
                var textElement = new TextElement()
                {
                    text = "One or more randomizers have missing scripts. See console for more info."
                };
                textElement.AddToClassList("scenario__info-box");
                textElement.AddToClassList("scenario__error-box");
                m_Container.Add(textElement);
                return;
            }

            if (m_Property.arraySize == 0)
            {
                var textElement = new TextElement()
                {
                    text = "No randomizers added. Add one below!"
                };
                textElement.AddToClassList("scenario__warning-box");
                m_Container.Add(textElement);
            }

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

        void ClearNullRandomizers()
        {
            Undo.RegisterCompleteObjectUndo(m_Property.serializedObject.targetObject, "Clear Null Randomizers");
            scenario.ClearNullRandomizers();
            #if UNITY_2021_3_OR_NEWER
            SerializationUtility.ClearAllManagedReferencesWithMissingTypes(scenario);
            #endif
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

        void SetRandomizersCollapsedState(bool collapsed)
        {
            foreach (var child in m_Container.Children())
                if (child is RandomizerElement re)
                    re.collapsed = collapsed;
        }
    }
}
