using System;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization
{
    class DrawerParameterElement : VisualElement
    {
        const string k_CollapsedParameterClass = "collapsed";
        SerializedProperty m_Collapsed;
        SerializedProperty m_Property;

        public DrawerParameterElement(SerializedProperty property)
        {
            m_Property = property;
            m_Collapsed = property.FindPropertyRelative("collapsed");

            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/Parameter/ParameterDrawer.uxml").CloneTree(this);

            var collapseToggle = this.Q<VisualElement>("collapse");
            collapseToggle.RegisterCallback<MouseUpEvent>(evt => collapsed = !collapsed);
            collapsed = m_Collapsed.boolValue;

            var fieldNameField = this.Q<Label>("field-name");
            fieldNameField.text = property.displayName;

            var drawer = this.Q<VisualElement>("drawer");
            drawer.Add(new ParameterElement(property));
        }

        bool collapsed
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
    }
}
