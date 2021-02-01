using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    [CustomEditor(typeof(RandomizerTag), true)]
    public class RandomizerTagEditor : UnityEditor.Editor
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
