using System;
using UnityEngine;
using UnityEngine.Experimental.Perception.Randomization.Samplers;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.Perception.Randomization.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(FloatRange))]
    class FloatRangeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new FloatRangeElement(property);
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
