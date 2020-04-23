using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(Labeling))]
    class LabelingEditor : Editor
    {
        const int k_Indent = 7;
        ReorderableList m_LabelsList;

        public void OnEnable()
        {
            m_LabelsList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(global::UnityEngine.Perception.GroundTruth.Labeling.labels)), true, false, true, true);
            m_LabelsList.drawElementCallback = DrawElement;
            m_LabelsList.onAddCallback += OnAdd;
            m_LabelsList.onRemoveCallback += OnRemove;
        }

        void OnRemove(ReorderableList list)
        {
            if (list.index != -1)
                Labeling.labels.RemoveAt(list.index);
        }

        Labeling Labeling => (Labeling)target;

        void OnAdd(ReorderableList list)
        {
            Labeling.labels.Add("");
        }

        void DrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var indent = k_Indent * index;
                if (indent >= rect.width)
                    return;

                var contentRect = new Rect(rect.x + indent, rect.y, rect.width - indent, rect.height);
                var value = EditorGUI.TextField(contentRect, Labeling.labels[index]);
                if (change.changed)
                {
                    Labeling.labels[index] = value;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            m_LabelsList.DoLayoutList();
        }
    }
}
