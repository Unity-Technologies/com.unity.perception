using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Experimental.Perception.Randomization.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.Experimental.Perception.Randomization.VisualElements
{
    class RandomizerElement : VisualElement
    {
        SerializedProperty m_Collapsed;
        SerializedProperty m_Property;
        VisualElement m_PropertiesContainer;

        Randomizers.Randomizer randomizer => (Randomizers.Randomizer)StaticData.GetManagedReferenceValue(m_Property);

        public Type randomizerType => randomizer.GetType();

        const string k_CollapsedParameterClass = "collapsed";

        public RandomizerList randomizerList { get; }

        public bool collapsed
        {
            get => m_Collapsed.boolValue;
            set
            {
                m_Collapsed.boolValue = value;
                m_Property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                if (value)
                    AddToClassList(k_CollapsedParameterClass);
                else
                    RemoveFromClassList(k_CollapsedParameterClass);
            }
        }

        public RandomizerElement(SerializedProperty property, RandomizerList randomizerList)
        {
            m_Property = property;
            this.randomizerList = randomizerList;
            m_Collapsed = property.FindPropertyRelative("collapsed");
            collapsed = m_Collapsed.boolValue;

            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/Randomizer/RandomizerElement.uxml").CloneTree(this);

            var classNameLabel = this.Q<TextElement>("class-name");
            var splitType = property.managedReferenceFullTypename.Split(' ', '.');
            classNameLabel.text = splitType[splitType.Length - 1];

            m_PropertiesContainer = this.Q<VisualElement>("properties");

            var collapseToggle = this.Q<VisualElement>("collapse");
            collapseToggle.RegisterCallback<MouseUpEvent>(evt => collapsed = !collapsed);

            var enabledToggle = this.Q<Toggle>("enabled");
            enabledToggle.BindProperty(property.FindPropertyRelative("<enabled>k__BackingField"));

            var removeButton = this.Q<Button>("remove");
            removeButton.clicked += () => randomizerList.RemoveRandomizer(this);

            this.AddManipulator(new DragToReorderManipulator());

            FillPropertiesContainer();
        }

        void FillPropertiesContainer()
        {
            m_PropertiesContainer.Clear();
            var iterator = m_Property.Copy();
            var nextSiblingProperty = m_Property.Copy();
            nextSiblingProperty.NextVisible(false);

            var foundProperties = false;
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, nextSiblingProperty))
                        break;
                    if (iterator.name == "<enabled>k__BackingField")
                        continue;
                    foundProperties = true;
                    var propertyField = new PropertyField(iterator.Copy());
                    propertyField.Bind(m_Property.serializedObject);
                    m_PropertiesContainer.Add(propertyField);
                } while (iterator.NextVisible(false));
            }

            if (!foundProperties)
                m_PropertiesContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }
    }
}
