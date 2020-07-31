using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    public class RandomSeedField : IntegerField
    {
        public RandomSeedField(SerializedProperty property)
        {
            label = "Seed";
            this.BindProperty(property);
            this.RegisterValueChangedCallback((e) =>
            {
                if (e.newValue <= 0)
                {
                    value = 0;
                    binding.Update();
                    e.StopImmediatePropagation();
                }
            });
        }
    }
}
