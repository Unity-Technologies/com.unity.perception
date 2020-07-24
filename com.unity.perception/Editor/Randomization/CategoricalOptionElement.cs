using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    public class CategoricalOptionElement : VisualElement
    {
        public CategoricalOptionElement()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/CategoricalOptionElement.uxml");
            template.CloneTree(this);
        }

        public void BindProperties(int index, SerializedProperty optionProperty, SerializedProperty probabilityProperty)
        {
            var indexLabel = this.Q<Label>("index-label");
            indexLabel.text = $"[{index}]";

            var option = this.Q<PropertyField>("option");
            option.BindProperty(optionProperty);
            var label = option.Q<Label>();
            label.parent.Remove(label);

            var probability = this.Q<FloatField>("probability");
            probability.BindProperty(probabilityProperty);
            probability.label = string.Empty;
        }
    }
}
