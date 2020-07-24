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

        public CategoricalOptionElement(SerializedProperty optionsProperty, SerializedProperty probabilitiesProperty, ListView view)
        {
            m_OptionsProperty = optionsProperty;
            m_ProbabilitiesProperty = probabilitiesProperty;

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/CategoricalOptionElement.uxml");
            template.CloneTree(this);

            var removeButton = this.Q<Button>("remove");
            removeButton.clicked += () =>
            {
                optionsProperty.DeleteArrayElementAtIndex(m_Index);
                probabilitiesProperty.DeleteArrayElementAtIndex(m_Index);
                optionsProperty.serializedObject.ApplyModifiedProperties();
                view.Refresh();
            };
        }

        public void BindProperties(int i)
        {
            m_Index = i;
            var indexLabel = this.Q<Label>("index-label");
            indexLabel.text = $"[{m_Index}]";

            var optionProperty = m_OptionsProperty.GetArrayElementAtIndex(i);
            var option = this.Q<PropertyField>("option");
            option.BindProperty(optionProperty);
            var label = option.Q<Label>();
            label.parent.Remove(label);

            var probabilityProperty = m_ProbabilitiesProperty.GetArrayElementAtIndex(i);
            var probability = this.Q<FloatField>("probability");
            probability.BindProperty(probabilityProperty);
            probability.label = string.Empty;
        }
    }
}
