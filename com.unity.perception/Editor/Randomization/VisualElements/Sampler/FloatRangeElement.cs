using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization
{
    class FloatRangeElement : VisualElement
    {
        public FloatRangeElement(SerializedProperty property)
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/Sampler/FloatRangeElement.uxml");
            template.CloneTree(this);

            var minimumField = this.Q<FloatField>("minimum");
            minimumField.bindingPath = property.propertyPath + ".minimum";

            var maximumField = this.Q<FloatField>("maximum");
            maximumField.bindingPath = property.propertyPath + ".maximum";

            this.Bind(property.serializedObject);
        }
    }
}
