using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.Experimental.Perception.Editor
{
    /// <summary>
    /// Derive this class to force the Unity Editor to render an Object's default inspector using UIElements
    /// </summary>
    public abstract class DefaultUIElementsEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var rootElement = new VisualElement();
            CreatePropertyFields(rootElement);
            return rootElement;
        }

        void CreatePropertyFields(VisualElement rootElement)
        {
            var iterator = serializedObject.GetIterator();
            iterator.NextVisible(true);
            do
            {
                if (iterator.name == "m_Script")
                    continue;
                var propertyField = new PropertyField(iterator.Copy());
                propertyField.Bind(serializedObject);
                rootElement.Add(propertyField);
            } while (iterator.NextVisible(false));
        }
    }
}
