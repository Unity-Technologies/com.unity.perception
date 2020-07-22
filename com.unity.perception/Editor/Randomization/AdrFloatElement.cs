using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    public class AdrFloatElement : VisualElement
    {
        public AdrFloatElement(SerializedProperty property)
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{StaticData.uxmlDir}/AdrFloatElement.uxml");
            template.CloneTree(this);
            this.Bind(property.serializedObject);
            var seedField = this.Q<IntegerField>("seed");
            seedField.RegisterValueChangedCallback((e) =>
            {
                if (e.newValue <= 0)
                {
                    seedField.value = 0;
                    property.FindPropertyRelative("baseRandomSeed").intValue = 0;
                    property.serializedObject.ApplyModifiedProperties();
                    e.StopImmediatePropagation();
                }
            });
        }
    }
}
