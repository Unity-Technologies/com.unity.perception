using System.Linq;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.LabelManagement;

namespace UnityEditor.Perception.GroundTruth
{
    /// <summary>
    /// Editor check for GameObject parent has a Labeling component
    /// </summary>
    [CustomEditor(typeof(JointLabel))]
    [CanEditMultipleObjects]
    public class JointLabelEditor : Editor
    {
        /// <summary>
        /// Editor check for GameObject parent has a Labeling component
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
#if UNITY_2020_1_OR_NEWER
            //GetComponentInParent<T>(bool includeInactive) only exists on 2020.1 and later
            if (targets.Any(t => ((Component)t).gameObject.GetComponentInParent<Labeling>(true) == null))
#else
            if (targets.Any(t => ((Component)t).GetComponentInParent<Labeling>() == null))
#endif
                EditorGUILayout.HelpBox("No Labeling component detected on parents. Keypoint labeling requires a Labeling component on the root of the object.", MessageType.Info);
        }
    }
}
