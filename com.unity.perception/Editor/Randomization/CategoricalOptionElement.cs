using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    public class CategoricalOptionElement : VisualElement
    {
        int m_Index;
        SerializedProperty m_OptionsProperty;
        SerializedProperty m_ProbabilitiesProperty;

        public CategoricalOptionElement(
            SerializedProperty optionsProperty,
            SerializedProperty probabilitiesProperty)
        {
            m_OptionsProperty = optionsProperty;
            m_ProbabilitiesProperty = probabilitiesProperty;

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/CategoricalOptionElement.uxml");
            template.CloneTree(this);
        }

        // Called from categorical parameter
        public void BindProperties(int i)
        {
            m_Index = i;
            var indexLabel = this.Q<Label>("index-label");
            indexLabel.text = $"[{m_Index}]";

            var probabilityProperty = m_ProbabilitiesProperty.GetArrayElementAtIndex(i);
            var probability = this.Q<FloatField>("probability");
            probability.RegisterValueChangedCallback((evt) =>
            {
                if (evt.newValue < 0f)
                    probability.value = 0f;
            });
            probability.labelElement.style.minWidth = 0;
            probability.labelElement.style.marginRight = 4;
            probability.BindProperty(probabilityProperty);

            var optionProperty = m_OptionsProperty.GetArrayElementAtIndex(i);
            var option = this.Q<PropertyField>("option");
            option.BindProperty(optionProperty);
            var label = option.Q<Label>();
            label.parent.Remove(label);
        }
    }
}
