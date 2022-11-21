using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Perception.UIElements;

namespace UnityEditor.Perception.Randomization.PropertyDrawers
{
    /// <summary>
    /// Creates proper VisualElement for the uint data
    /// </summary>
    [CustomPropertyDrawer(typeof(uint))]
    public class UIntDrawer : PropertyDrawer
    {
        /// <summary>
        /// Create proper Visual Element for the uint data
        /// </summary>
        /// <param name="property">Property to draw</param>
        /// <returns>Proper UI representation for the uint data</returns>
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

        /// <summary>
        /// Draws the property
        /// </summary>
        /// <param name="position">Area to draw</param>
        /// <param name="property">Property to draw</param>
        /// <param name="label">Label to use</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        /// <summary>
        /// Returns the proper height for the drawable property
        /// </summary>
        /// <param name="property">Property to calculate</param>
        /// <param name="label">Label to use</param>
        /// <returns>Height of the visual element as float</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }
    }
}
