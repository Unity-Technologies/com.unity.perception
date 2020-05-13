using System;
using Unity.Mathematics;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(IdLabelConfig))]
    class IdLabelConfigEditor : Editor
    {
        ReorderableList m_LabelsList;
        const float k_Margin = 5f;

        public void OnEnable()
        {
            m_LabelsList = new ReorderableList(this.serializedObject, this.serializedObject.FindProperty(IdLabelConfig.labelEntriesFieldName), true, false, true, true);
            m_LabelsList.elementHeight = EditorGUIUtility.singleLineHeight * 2 + k_Margin;
            m_LabelsList.drawElementCallback = DrawElement;
            m_LabelsList.onAddCallback += OnAdd;
            m_LabelsList.onRemoveCallback += OnRemove;
            m_LabelsList.onReorderCallbackWithDetails += OnReorder;
        }

        void OnReorder(ReorderableList list, int oldIndex, int newIndex)
        {
            if (!autoAssign)
                return;

            AutoAssignIds();
        }

        void OnRemove(ReorderableList list)
        {
            if (list.index != -1)
                list.serializedProperty.DeleteArrayElementAtIndex(list.index);

            if (autoAssign)
                AutoAssignIds();

            this.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        void OnAdd(ReorderableList list)
        {
            int maxLabel = Int32.MinValue;
            if (list.serializedProperty.arraySize == 0)
                maxLabel = -1;

            for (int i = 0; i < list.serializedProperty.arraySize; i++)
            {
                var item = list.serializedProperty.GetArrayElementAtIndex(i);
                maxLabel = math.max(maxLabel, item.FindPropertyRelative(nameof(IdLabelEntry.id)).intValue);
            }
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.InsertArrayElementAtIndex(index);
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            var idProperty = element.FindPropertyRelative(nameof(IdLabelEntry.id));
            idProperty.intValue = maxLabel + 1;
            var labelProperty = element.FindPropertyRelative(nameof(IdLabelEntry.label));
            labelProperty.stringValue = "";

            if (autoAssign)
                AutoAssignIds();

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        void DrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            var element = m_LabelsList.serializedProperty.GetArrayElementAtIndex(index);
            var idProperty = element.FindPropertyRelative(nameof(IdLabelEntry.id));
            var labelProperty = element.FindPropertyRelative(nameof(IdLabelEntry.label));
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var contentRect = new Rect(rect.position, new Vector2(rect.width, EditorGUIUtility.singleLineHeight));
                using (new EditorGUI.DisabledScope(autoAssign))
                {
                    var newLabel = EditorGUI.IntField(contentRect, nameof(IdLabelEntry.id), idProperty.intValue);
                    if (change.changed)
                    {
                        idProperty.intValue = newLabel;
                        if (autoAssign)
                            AutoAssignIds();
                    }
                }
            }
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var contentRect = new Rect(rect.position + new Vector2(0, EditorGUIUtility.singleLineHeight), new Vector2(rect.width, EditorGUIUtility.singleLineHeight));
                var newLabel = EditorGUI.TextField(contentRect, nameof(IdLabelEntry.label), labelProperty.stringValue);
                if (change.changed)
                {
                    labelProperty.stringValue = newLabel;
                }
            }
        }

        bool autoAssign => serializedObject.FindProperty(nameof(IdLabelConfig.autoAssignIds)).boolValue;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var autoAssignIdsProperty = serializedObject.FindProperty(nameof(IdLabelConfig.autoAssignIds));
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(autoAssignIdsProperty, new GUIContent("Auto Assign IDs"));
                if (change.changed && autoAssignIdsProperty.boolValue)
                    AutoAssignIds();
            }

            if (autoAssignIdsProperty.boolValue)
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var startingLabelIdProperty = serializedObject.FindProperty(nameof(IdLabelConfig.startingLabelId));
                    EditorGUILayout.PropertyField(startingLabelIdProperty, new GUIContent("Starting Label ID"));
                    if (change.changed)
                        AutoAssignIds();
                }
            }

            m_LabelsList.DoLayoutList();
            this.serializedObject.ApplyModifiedProperties();
        }

        void AutoAssignIds()
        {
            var serializedProperty = serializedObject.FindProperty(IdLabelConfig.labelEntriesFieldName);
            var size = serializedProperty.arraySize;
            if (size == 0)
                return;

            var startingLabelId  = (StartingLabelId)serializedObject.FindProperty(nameof(IdLabelConfig.startingLabelId)).enumValueIndex;

            var nextId = startingLabelId == StartingLabelId.One ? 1 : 0;
            for (int i = 0; i < size; i++)
            {
                serializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(IdLabelEntry.id)).intValue = nextId;
                nextId++;
            }
        }
    }
}
