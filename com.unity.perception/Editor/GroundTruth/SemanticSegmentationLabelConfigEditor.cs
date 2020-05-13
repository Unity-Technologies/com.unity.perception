using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using Random = UnityEngine.Random;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(SemanticSegmentationLabelConfig))]
    class SemanticSegmentationLabelConfigEditor : Editor
    {
        ReorderableList m_LabelsList;
        const float k_Margin = 5f;

        static List<Color> s_StandardColors = new List<Color>()
        {
            Color.blue,
            Color.green,
            Color.red,
            Color.white,
            Color.yellow,
            Color.gray
        };

        public void OnEnable()
        {
            m_LabelsList = new ReorderableList(this.serializedObject, this.serializedObject.FindProperty(IdLabelConfig.labelEntriesFieldName), true, false, true, true);
            m_LabelsList.elementHeight = EditorGUIUtility.singleLineHeight * 2 + k_Margin;
            m_LabelsList.drawElementCallback = DrawElement;
            m_LabelsList.onAddCallback += OnAdd;
        }

        void OnAdd(ReorderableList list)
        {
            var standardColorList = new List<Color>(s_StandardColors);
            for (int i = 0; i < list.serializedProperty.arraySize; i++)
            {
                var item = list.serializedProperty.GetArrayElementAtIndex(i);
                standardColorList.Remove(item.FindPropertyRelative(nameof(SemanticSegmentationLabelEntry.color)).colorValue);
            }
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.InsertArrayElementAtIndex(index);
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            var labelProperty = element.FindPropertyRelative(nameof(SemanticSegmentationLabelEntry.label));
            labelProperty.stringValue = "";
            var colorProperty = element.FindPropertyRelative(nameof(SemanticSegmentationLabelEntry.color));
            if (standardColorList.Any())
                colorProperty.colorValue = standardColorList.First();
            else
                colorProperty.colorValue = Random.ColorHSV(0, 1, .5f, 1, 1, 1);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        void DrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            var element = m_LabelsList.serializedProperty.GetArrayElementAtIndex(index);
            var colorProperty = element.FindPropertyRelative(nameof(SemanticSegmentationLabelEntry.color));
            var labelProperty = element.FindPropertyRelative(nameof(SemanticSegmentationLabelEntry.label));
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var contentRect = new Rect(rect.position, new Vector2(rect.width, EditorGUIUtility.singleLineHeight));
                var newLabel = EditorGUI.TextField(contentRect, nameof(SemanticSegmentationLabelEntry.label), labelProperty.stringValue);
                if (change.changed)
                {
                    labelProperty.stringValue = newLabel;
                }
            }
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var contentRect = new Rect(rect.position + new Vector2(0, EditorGUIUtility.singleLineHeight), new Vector2(rect.width, EditorGUIUtility.singleLineHeight));
                var newLabel = EditorGUI.ColorField(contentRect, nameof(SemanticSegmentationLabelEntry.color), colorProperty.colorValue);
                if (change.changed)
                {
                    colorProperty.colorValue = newLabel;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_LabelsList.DoLayoutList();
            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
