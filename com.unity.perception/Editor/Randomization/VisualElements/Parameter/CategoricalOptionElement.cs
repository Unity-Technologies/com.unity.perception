using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.Experimental.Perception.Randomization.Editor
{
    class CategoricalOptionElement : VisualElement
    {
        int m_Index;
        SerializedProperty m_CategoryProperty;
        SerializedProperty m_ProbabilitiesProperty;

        internal CategoricalOptionElement(
            SerializedProperty categoryProperty,
            SerializedProperty probabilitiesProperty)
        {
            m_CategoryProperty = categoryProperty;
            m_ProbabilitiesProperty = probabilitiesProperty;

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/Parameter/CategoricalOptionElement.uxml");
            template.CloneTree(this);
        }

        // Called from categorical parameter
        public void BindProperties(int i)
        {
            m_Index = i;
            var indexLabel = this.Q<Label>("index-label");
            indexLabel.text = $"[{m_Index}]";

            var optionProperty = m_CategoryProperty.GetArrayElementAtIndex(i);
            var option = this.Q<PropertyField>("option");
            option.BindProperty(optionProperty);
            var label = option.Q<Label>();
            label.parent.Remove(label);

            var probabilityProperty = m_ProbabilitiesProperty.GetArrayElementAtIndex(i);
            var probability = this.Q<FloatField>("probability");
            probability.isDelayed = true;
            probability.labelElement.style.minWidth = 0;
            probability.labelElement.style.marginRight = 4;
            if (Application.isPlaying)
            {
                probability.value = probabilityProperty.floatValue;
                probability.SetEnabled(false);
            }
            else
            {
                probability.SetEnabled(true);
                probability.RegisterValueChangedCallback((evt) =>
                {
                    if (evt.newValue < 0f)
                        probability.value = 0f;
                });
                probability.BindProperty(probabilityProperty);
            }
        }
    }
}
