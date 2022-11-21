using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using Random = UnityEngine.Random;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(KeypointTemplate))]
    class KeypointTemplateEditor : Editor
    {
        ReorderableList m_KeypointsList;
        ReorderableList m_SkeletonList;
        private const float k_Indent = 10;

        SerializedProperty keypointsProperty => this.serializedObject.FindProperty(nameof(KeypointTemplate.keypoints));
        SerializedProperty skeletonProperty => this.serializedObject.FindProperty(nameof(KeypointTemplate.skeleton));

        private KeypointTemplate targetObject => ((KeypointTemplate)serializedObject.targetObject);
        public void OnEnable()
        {
            m_KeypointsList = new ReorderableList(this.serializedObject, keypointsProperty, true, false, true, true);
            m_KeypointsList.drawHeaderCallback = (rect) =>
            {
                EditorGUI.LabelField(rect, "Keypoints", EditorStyles.largeLabel);
            };
            m_KeypointsList.onAddCallback += OnAddKeypointDefinition;
            m_KeypointsList.elementHeightCallback =
                i => EditorGUI.GetPropertyHeight(keypointsProperty.GetArrayElementAtIndex(i));
            m_KeypointsList.drawElementCallback += (rect, index, active, focused) =>
            {
                rect.xMin += k_Indent;
                EditorGUI.PropertyField(rect, keypointsProperty.GetArrayElementAtIndex(index), true);
            };
            m_SkeletonList = new ReorderableList(this.serializedObject, skeletonProperty, true, false, true, true);
            m_SkeletonList.drawHeaderCallback = (rect) =>
            {
                EditorGUI.LabelField(rect, "Skeleton", EditorStyles.largeLabel);
            };
            m_SkeletonList.onAddCallback += OnAddSkeletonDefinition;
            m_SkeletonList.drawElementCallback += (rect, index, active, focused) =>
            {
                rect.xMin += k_Indent;
                EditorGUI.PropertyField(rect, skeletonProperty.GetArrayElementAtIndex(index), true);
            };

            m_SkeletonList.elementHeightCallback =
                i => EditorGUI.GetPropertyHeight(skeletonProperty.GetArrayElementAtIndex(i));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(KeypointTemplate.templateID)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(KeypointTemplate.templateName)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(KeypointTemplate.jointTexture)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(KeypointTemplate.occludedJointTexture)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(KeypointTemplate.occludedJointColor)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(KeypointTemplate.skeletonTexture)));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.LabelField(L10n.Tr("Key Points"));
            m_KeypointsList.DoLayoutList();
            EditorGUILayout.LabelField(L10n.Tr("Skeletons"));
            m_SkeletonList.DoLayoutList();
        }

        void OnAddKeypointDefinition(ReorderableList list)
        {
            Undo.RegisterCompleteObjectUndo(target, "Add Keypoint Definition");
            AddDefinitionToProperty(keypointsProperty);
        }

        void OnAddSkeletonDefinition(ReorderableList list)
        {
            Undo.RegisterCompleteObjectUndo(target, "Add Skeleton Definition");
            AddDefinitionToProperty(skeletonProperty);
        }

        void AddDefinitionToProperty(SerializedProperty property)
        {
            var nextIndex = property.arraySize;
            property.InsertArrayElementAtIndex(nextIndex);
            var arrayElementAtIndex = property.GetArrayElementAtIndex(nextIndex);
            var generateRandomColor = false;

            Color GetUniqueRandomColor()
            {
                var duplicateDetected = true;
                var newColor = Color.clear;
                while (duplicateDetected)
                {
                    newColor = Random.ColorHSV(
                        0f, 1f,
                        0.5f, 1f, // values less than 0.5 are too dark
                        0.5f, 1f // values less than 0.5 are too dark
                    );

                    duplicateDetected = false;
                    for (var i = 0; i < property.arraySize; i++)
                    {
                        if (duplicateDetected)
                            break;

                        var elementAtI = property.GetArrayElementAtIndex(i);
                        var elementAtIColor = elementAtI.FindPropertyRelative("color");

                        if (elementAtIColor != null && elementAtIColor.colorValue == newColor)
                            duplicateDetected = true;
                    }
                }

                return newColor;
            }

            // when we insert a new element, it copies values from the previous element
            // if color of this element and last element are the same, pick a random new color
            // if this is the first element of the list, also pick a random new color
            var colorProperty = arrayElementAtIndex.FindPropertyRelative("color");
            if (nextIndex <= 0)
            {
                generateRandomColor = true;
            }
            else
            {
                var arrayElementAtIndexMinus1 = property.GetArrayElementAtIndex(nextIndex - 1);
                if (arrayElementAtIndexMinus1.FindPropertyRelative("color").colorValue == colorProperty.colorValue)
                    generateRandomColor = true;
            }
            if (generateRandomColor)
            {
                colorProperty.colorValue = GetUniqueRandomColor();
            }

            arrayElementAtIndex.isExpanded = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
