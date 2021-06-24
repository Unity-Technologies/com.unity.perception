using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Perception.Randomization.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(uint))]
    class UIntDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new UIntField
            {
                label = property.displayName,
                value = (uint)property.longValue
            };

            //Binding does not work on this custom UI Element field that we have created, so we need to use the change event
            field.RegisterValueChangedCallback(evt =>
            {
                field.value = evt.newValue;
                property.longValue = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });

            // Create a surrogate integer field to detect and pass along external change events (non UI event) on the underlying serialized property.
            var surrogateField = new IntegerField();
            field.Add(surrogateField);
            surrogateField.style.display = DisplayStyle.None;
            surrogateField.bindingPath = property.propertyPath;
            surrogateField.RegisterValueChangedCallback(evt =>
            {
                evt.StopImmediatePropagation();
                field.value = UIntField.ClampInput(property.longValue);
            });

            return field;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }
    }
}
