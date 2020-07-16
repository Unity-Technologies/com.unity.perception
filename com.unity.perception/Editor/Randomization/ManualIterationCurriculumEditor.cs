using UnityEditor;
using UnityEngine.Perception.Randomization.Curriculum;

namespace UnityEngine.Perception.Randomization.Samplers.Editor
{
    [CustomEditor(typeof(ManualIterationCurriculum))]
    public class ManualIterationCurriculumEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var curriculum = (ManualIterationCurriculum)target;
            base.OnInspectorGUI();
            if (GUILayout.Button("Next Iteration"))
                curriculum.ManuallyFinishIteration();
        }
    }
}
