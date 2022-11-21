using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ISampler), true)]
    class SamplerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return fieldInfo.FieldType.IsInterface
                ? new SamplerInterfaceElement(property) as VisualElement
                : new SamplerElement(property);
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
