using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    public class FloatRangeElement : VisualElement
    {
        public FloatRangeElement(SerializedProperty property)
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{StaticData.uxmlDir}/FloatRangeElement.uxml");
            template.CloneTree(this);
            this.Bind(property.serializedObject);
        }
    }
}
