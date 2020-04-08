using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(Labeling))]
public class LabelingEditor : Editor
{
    const int k_Indent = 7;
    ReorderableList m_LabelsList;

    public void OnEnable()
    {
        m_LabelsList = new ReorderableList(this.serializedObject, this.serializedObject.FindProperty(nameof(Labeling.classes)), true, false, true, true);
        m_LabelsList.drawElementCallback = DrawElement;
        m_LabelsList.onAddCallback += OnAdd;
        m_LabelsList.onRemoveCallback += OnRemove;
    }

    void OnRemove(ReorderableList list)
    {
        if (list.index != -1)
            labeling.classes.RemoveAt(list.index);
    }

    Labeling labeling => (Labeling)this.target;

    void OnAdd(ReorderableList list)
    {
        labeling.classes.Add("");
    }

    void DrawElement(Rect rect, int index, bool isactive, bool isfocused)
    {
        using (var change = new EditorGUI.ChangeCheckScope())
        {
            var indent = k_Indent * index;
            if (indent >= rect.width)
                return;

            var contentRect = new Rect(rect.x + indent, rect.y, rect.width - indent, rect.height);
            var value = EditorGUI.TextField(contentRect, labeling.classes[index]);
            if (change.changed)
            {
                labeling.classes[index] = value;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        m_LabelsList.DoLayoutList();
    }
}
