using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.Sensors;

[CustomEditor(typeof(LabelingConfiguration))]
public class LabelingConfigurationEditor : Editor
{
    ReorderableList m_LabelsList;

    public void OnEnable()
    {
        m_LabelsList = new ReorderableList(this.serializedObject, this.serializedObject.FindProperty(nameof(LabelingConfiguration.LabelingConfigurations)), true, false, true, true);
        m_LabelsList.elementHeight = EditorGUIUtility.singleLineHeight * 2;
        m_LabelsList.drawElementCallback = DrawElement;
        m_LabelsList.onAddCallback += OnAdd;
        m_LabelsList.onRemoveCallback += OnRemove;
    }

    void OnRemove(ReorderableList list)
    {
        if (list.index != -1)
            config.LabelingConfigurations.RemoveAt(list.index);
    }

    LabelingConfiguration config => (LabelingConfiguration)this.target;

    void OnAdd(ReorderableList list)
    {
        config.LabelingConfigurations.Add(new LabelingConfigurationEntry("", 0));
    }

    void DrawElement(Rect rect, int index, bool isactive, bool isfocused)
    {
        var entry = config.LabelingConfigurations[index];
        using (var change = new EditorGUI.ChangeCheckScope())
        {
            var contentRect = new Rect(rect.position, new Vector2(rect.width, EditorGUIUtility.singleLineHeight));
            var newLabel = EditorGUI.TextField(contentRect, nameof(LabelingConfigurationEntry.label), entry.label);
            if (change.changed)
                config.LabelingConfigurations[index] = new LabelingConfigurationEntry(newLabel, entry.value);
        }
        using (var change = new EditorGUI.ChangeCheckScope())
        {
            var contentRect = new Rect(rect.position + new Vector2(0, EditorGUIUtility.singleLineHeight), new Vector2(rect.width, EditorGUIUtility.singleLineHeight));
            var newValue = EditorGUI.IntField(contentRect, nameof(LabelingConfigurationEntry.value), entry.value);
            if (change.changed)
                config.LabelingConfigurations[index] = new LabelingConfigurationEntry(entry.label, newValue);
        }
    }

    public override void OnInspectorGUI()
    {
        m_LabelsList.DoLayoutList();
    }
}
