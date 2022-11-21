using UnityEditor.Perception.Randomization.VisualElements.AssetSource;
using UnityEngine;
using UnityEngine.Perception.Randomization;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(AssetSource<>))]
    class AssetSourceDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new AssetSourceElement(property, fieldInfo);
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
