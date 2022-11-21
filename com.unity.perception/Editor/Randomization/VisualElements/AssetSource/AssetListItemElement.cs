using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization.VisualElements.AssetSource
{
    class AssetListItemElement : VisualElement
    {
        int m_Index;
        Type m_ItemType;
        SerializedProperty m_Property;

        public AssetListItemElement(SerializedProperty property, Type itemType)
        {
            m_Property = property;
            m_ItemType = itemType;
        }

        public void BindProperties(int i)
        {
            Clear();
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/AssetSource/{nameof(AssetListItemElement)}.uxml");
            template.CloneTree(this);

            m_Index = i;
            var indexLabel = this.Q<Label>("index-label");
            indexLabel.text = $"[{m_Index}]";

            var optionProperty = m_Property.GetArrayElementAtIndex(i);
            var option = this.Q<ObjectField>("item");
            option.BindProperty(optionProperty);
            option.objectType = m_ItemType;
            option.allowSceneObjects = false;
        }
    }
}
