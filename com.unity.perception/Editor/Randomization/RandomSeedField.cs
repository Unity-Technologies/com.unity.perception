using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.Experimental.Perception.Randomization.Editor
{
    class RandomSeedField : IntegerField
    {
        public RandomSeedField(SerializedProperty property)
        {
            label = "Seed";
            this.BindProperty(property);
        }
    }
}
