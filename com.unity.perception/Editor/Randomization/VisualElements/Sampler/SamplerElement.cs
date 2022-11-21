using System;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization
{
    class SamplerElement : VisualElement
    {
        public SamplerElement(SerializedProperty property)
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/Sampler/SamplerElement.uxml");
            template.CloneTree(this);

            var displayName = this.Q<Label>("display-name");
            displayName.text = property.displayName;

            var fieldsContainer = this.Q<VisualElement>("fields-container");
            UIElementsEditorUtilities.CreatePropertyFields(property, fieldsContainer);
        }
    }
}
