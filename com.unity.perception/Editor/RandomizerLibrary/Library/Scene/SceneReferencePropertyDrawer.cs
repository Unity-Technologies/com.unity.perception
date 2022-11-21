using UnityEditor.UIElements;
using UnityEngine.Perception.Utilities;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization.Randomizers
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    [MovedFrom("UnityEditor.Perception.Internal")]
    class SceneReferencePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new PropertyField(property.FindPropertyRelative("sceneAsset"))
            {
                label = "Scene Reference",
                style =
                {
                    height = 44
                }
            };
        }
    }
}
