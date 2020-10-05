using UnityEditor;
using UnityEngine.Experimental.Perception.Randomization.Editor;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    class DrawerParameterElement : VisualElement
    {
        Parameter m_Parameter;
        SerializedProperty m_Collapsed;
        SerializedProperty m_Property;
        const string k_CollapsedParameterClass = "collapsed";

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
    }
}
