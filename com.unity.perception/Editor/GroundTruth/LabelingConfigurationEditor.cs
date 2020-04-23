using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(LabelingConfiguration))]
    class LabelingConfigurationEditor : Editor
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
                list.serializedProperty.DeleteArrayElementAtIndex(list.index);

            this.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        void OnAdd(ReorderableList list)
        {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.InsertArrayElementAtIndex(index);
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            var labelProperty = element.FindPropertyRelative(nameof(LabelingConfigurationEntry.label));
            labelProperty.stringValue = "";
            var valueProperty = element.FindPropertyRelative(nameof(LabelingConfigurationEntry.value));
            valueProperty.intValue = 0;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        void DrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            var element = m_LabelsList.serializedProperty.GetArrayElementAtIndex(index);
            var labelProperty = element.FindPropertyRelative(nameof(LabelingConfigurationEntry.label));
            var valueProperty = element.FindPropertyRelative(nameof(LabelingConfigurationEntry.value));
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var contentRect = new Rect(rect.position, new Vector2(rect.width, EditorGUIUtility.singleLineHeight));
                var newLabel = EditorGUI.TextField(contentRect, nameof(LabelingConfigurationEntry.label), labelProperty.stringValue);
                if (change.changed)
                    labelProperty.stringValue = newLabel;
            }

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var contentRect = new Rect(rect.position + new Vector2(0, EditorGUIUtility.singleLineHeight), new Vector2(rect.width, EditorGUIUtility.singleLineHeight));
                var newValue = EditorGUI.IntField(contentRect, nameof(LabelingConfigurationEntry.value), valueProperty.intValue);
                if (change.changed)
                    valueProperty.intValue = newValue;
            }
        }

        public override void OnInspectorGUI()
        {
            m_LabelsList.DoLayoutList();
            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
