using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(KeypointTemplate))]
    class KeypointTemplateEditor: Editor
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(KeypointTemplate.skeletonTexture)));

            serializedObject.ApplyModifiedProperties();

            m_KeypointsList.DoLayoutList();
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

        private void AddDefinitionToProperty(SerializedProperty property)
        {
            var nextIndex = property.arraySize;
            property.InsertArrayElementAtIndex(nextIndex);
            var arrayElementAtIndex = property.GetArrayElementAtIndex(nextIndex);

            // set default color to blue because Unity does not instantiate field values based on initializers
            var colorProperty = arrayElementAtIndex.FindPropertyRelative("color");
            if (colorProperty.colorValue == Color.clear)
                colorProperty.colorValue = Color.blue;

            arrayElementAtIndex.isExpanded = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
