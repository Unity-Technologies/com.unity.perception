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

            var minimumField = this.Q<FloatField>("minimum");
            minimumField.bindingPath = property.propertyPath + ".minimum";

            var maximumField = this.Q<FloatField>("maximum");
            maximumField.bindingPath = property.propertyPath + ".maximum";

            var defaultValueField = this.Q<FloatField>("defaultValue");
            defaultValueField.bindingPath = property.propertyPath + ".defaultValue";

            this.Bind(property.serializedObject);
        }
    }
}
