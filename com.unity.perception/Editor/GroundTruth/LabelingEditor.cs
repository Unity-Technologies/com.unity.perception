using Unity.Entities;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(Labeling)), CanEditMultipleObjects]
    class LabelingEditor : Editor
    {
        ReorderableList m_LabelsList;

        public void OnEnable()
        {
            m_LabelsList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(Labeling.labels)), true, false, true, true);
            m_LabelsList.drawElementCallback = DrawElement;
            m_LabelsList.onAddCallback += OnAdd;
            m_LabelsList.onRemoveCallback += OnRemove;
            m_LabelsList.onReorderCallbackWithDetails += OnReordered;
        }

        void OnRemove(ReorderableList list)
        {
            if (list.index != -1)
            {
                var value = labeling.labels[list.index];
                foreach (var t in targets)
                {
                    ((Labeling)t).labels.Remove(value);
                }
            }
        }

        Labeling labeling => (Labeling)target;

        void OnAdd(ReorderableList list)
        {
            foreach (var t in targets)
            {
                var castedTarget = ((Labeling)t);
                castedTarget.labels.Add("");
                EditorUtility.SetDirty(castedTarget);
            }
        }

        void OnReordered(ReorderableList list, int oldIndex, int newIndex)
        {
            var label = labeling.labels[newIndex];

            foreach (var t in targets)
            {
                var l = (Labeling)t;
                if (this.labeling == l) continue;

                ReorderLabels(l, label, newIndex);
            }
        }

        static void ReorderLabels(Labeling labeling, string label, int newIndex)
        {
            if (labeling.labels.Contains(label))
            {
                labeling.labels.Remove(label);
                if (newIndex < labeling.labels.Count)
                    labeling.labels.Insert(newIndex, label);
                else
                    labeling.labels.Add(label);
            }
        }

        static void ReplaceLabel(Labeling labeling, string oldLabel, string newLabel)
        {
            var idx = labeling.labels.IndexOf(oldLabel);
            if (idx == -1) return;
            labeling.labels[idx] = newLabel;
        }

        private void ReplaceLabel(int index, string newLabel)
        {
            labeling.labels[index] = newLabel;
        }

        void ReplaceLabelAll(int index, string currentLabel)
        {
            var oldLabel = labeling.labels[index];
            ReplaceLabel(index, currentLabel);

            foreach (var t in targets)
            {
                var l = (Labeling)t;

                if (this.labeling == l) continue;

                ReplaceLabel(l, oldLabel, currentLabel);
            }
        }

        void DrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var contentRect = new Rect(rect.x, rect.y, rect.width, rect.height);

                var value = EditorGUI.DelayedTextField(contentRect, labeling.labels[index]);

                if (change.changed)
                {
                    ReplaceLabelAll(index, value);

                    if (PrefabUtility.IsPartOfAnyPrefab(target))
                    {
                        EditorUtility.SetDirty(target);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            m_LabelsList.DoLayoutList();
        }
    }
}
