using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.UIElements;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(IdLabelConfig))]
    class IdLabelConfigEditor : Editor
    {
        private ListView m_LabelListView;

        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;

        private VisualElement m_Root;
        private IdLabelConfig m_IdLabelConig;
        private Button m_SaveButton;
        private Button m_AddNewLabelButton

        private List<string> m_AddedLabels = new List<string>();
        private SerializedProperty m_SerializedLabelsArray;
        public void OnEnable()
        {
            m_UxmlPath = m_UxmlDir + "LabelConfig_Main.uxml";
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree();
            m_LabelListView = m_Root.Q<ListView>("labels-listview");
            m_SaveButton = m_Root.Q<Button>("save-button");
            m_SaveButton.SetEnabled(false);
            m_IdLabelConig = (IdLabelConfig)target;
            m_SerializedLabelsArray = serializedObject.FindProperty(IdLabelConfig.labelEntriesFieldName);


            if (m_AutoAssign)
            {
                AutoAssignIds();
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            RefreshAddedLabels();
            SetupLabelsListView();
        }

        public override VisualElement CreateInspectorGUI()
        {
            serializedObject.Update();
            m_IdLabelConig = (IdLabelConfig)target;
            RefreshAddedLabels();
            m_LabelListView.Refresh();
            return m_Root;
        }

        void RefreshAddedLabels()
        {
            m_AddedLabels.Clear();
            m_AddedLabels.AddRange( m_IdLabelConig.labelEntries.Select(entry => entry.label));
        }
        void SetupLabelsListView()
        {
            m_LabelListView.itemsSource = m_AddedLabels;

            VisualElement MakeItem() =>
                new LabelElementInLabelConfig(this, m_SerializedLabelsArray, m_LabelListView);

            void BindItem(VisualElement e, int i)
            {
                if (e is LabelElementInLabelConfig addedLabel)
                {
                    addedLabel.m_IndexInList = i;
                    addedLabel.m_LabelTextField.BindProperty(m_SerializedLabelsArray.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(IdLabelEntry.label)));
                    addedLabel.m_LabelId.text = m_IdLabelConig.labelEntries[i].id.ToString();
                    addedLabel.UpdateMoveButtonVisibility(m_SerializedLabelsArray);
                }
            }

            const int itemHeight = 30;
            m_LabelListView.bindItem = BindItem;
            m_LabelListView.makeItem = MakeItem;
            m_LabelListView.itemHeight = itemHeight;
            m_LabelListView.selectionType = SelectionType.Single;

            m_LabelListView.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                RefreshListViewHeight();
            });


        }

        public void SetSaveButtonEnabled(bool enabled)
        {
            m_SaveButton.SetEnabled(enabled);
        }

        public void RefreshListViewHeight()
        {
            m_LabelListView.style.minHeight = Mathf.Clamp(m_LabelListView.itemsSource.Count * m_LabelListView.itemHeight, 300, 600);
        }

        bool m_AutoAssign => serializedObject.FindProperty(nameof(IdLabelConfig.autoAssignIds)).boolValue;
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

        void AddNewLabel(SerializedProperty serializedArray, HashSet<string> presentLabels)
        {
            int maxLabel = Int32.MinValue;
            if (serializedArray.arraySize == 0)
                maxLabel = -1;

            for (int i = 0; i < serializedArray.arraySize; i++)
            {
                var item = serializedArray.GetArrayElementAtIndex(i);
                maxLabel = math.max(maxLabel, item.FindPropertyRelative(nameof(IdLabelEntry.id)).intValue);
            }
            var index = serializedArray.arraySize;
            serializedArray.InsertArrayElementAtIndex(index);
            var element = serializedArray.GetArrayElementAtIndex(index);
            var idProperty = element.FindPropertyRelative(nameof(IdLabelEntry.id));
            idProperty.intValue = maxLabel + 1;
            var labelProperty = element.FindPropertyRelative(nameof(IdLabelEntry.label));
            labelProperty.stringValue = FindNewLabelValue(presentLabels);

            // if (m_AutoAssign)
            //     AutoAssignIds();

            serializedObject.ApplyModifiedProperties();
            //EditorUtility.SetDirty(target);
        }

        string FindNewLabelValue(HashSet<string> labels)
        {
            string baseLabel = "New Label";
            string label = baseLabel;
            int count = 1;
            while (labels.Contains(label))
            {
                label = baseLabel + "_" + count++;
            }
            return label;
        }
    }

    class LabelElementInLabelConfig : VisualElement
    {
        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;
        private Button m_RemoveButton;
        private Button m_MoveUpButton;
        private Button m_MoveDownButton;

        public TextField m_LabelTextField;
        public Label m_LabelId;
        public int m_IndexInList;


        public LabelElementInLabelConfig(IdLabelConfigEditor editor, SerializedProperty labelsArray, ListView labelsListView)
        {
            m_UxmlPath = m_UxmlDir + "LabelElementInLabelConfig.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree(this);
            m_LabelTextField = this.Q<TextField>("label-value");
            m_RemoveButton = this.Q<Button>("remove-button");
            m_MoveUpButton = this.Q<Button>("move-up-button");
            m_MoveDownButton = this.Q<Button>("move-down-button");
            m_LabelId = this.Q<Label>("label-id-value");


            m_MoveDownButton.clicked += () =>
            {
                if (m_IndexInList < labelsArray.arraySize - 1)
                {
                    var currentProperty =
                        labelsArray.GetArrayElementAtIndex(m_IndexInList).FindPropertyRelative(nameof(IdLabelEntry.label));
                    var bottomProperty = labelsArray.GetArrayElementAtIndex(m_IndexInList + 1)
                        .FindPropertyRelative("label");

                    var tmpString = bottomProperty.stringValue;
                    bottomProperty.stringValue = currentProperty.stringValue;
                    currentProperty.stringValue = tmpString;

                    m_IndexInList++;
                    labelsListView.SetSelection(m_IndexInList);
                    UpdateMoveButtonVisibility(labelsArray);

                    editor.serializedObject.ApplyModifiedProperties();

                    labelsListView.Refresh();
                    editor.RefreshListViewHeight();
                    //AssetDatabase.SaveAssets();
                }
            };


            m_MoveUpButton.clicked += () =>
            {
                if (m_IndexInList > 0)
                {
                    var currentProperty =
                        labelsArray.GetArrayElementAtIndex(m_IndexInList).FindPropertyRelative(nameof(IdLabelEntry.label));
                    var topProperty = labelsArray.GetArrayElementAtIndex(m_IndexInList - 1)
                        .FindPropertyRelative("label");

                    var tmpString = topProperty.stringValue;
                    topProperty.stringValue = currentProperty.stringValue;
                    currentProperty.stringValue = tmpString;

                    m_IndexInList--;
                    labelsListView.SetSelection(m_IndexInList);
                    UpdateMoveButtonVisibility(labelsArray);

                    editor.serializedObject.ApplyModifiedProperties();

                    labelsListView.Refresh();
                    editor.RefreshListViewHeight();
                    //AssetDatabase.SaveAssets();
                }
            };

            m_LabelTextField.RegisterValueChangedCallback<string>((cEvent) =>
            {
                editor.SetSaveButtonEnabled(EditorUtility.IsDirty(editor.target));
            });

            m_RemoveButton.clicked += () =>
            {

            };
        }

        public void UpdateMoveButtonVisibility(SerializedProperty labelsArray)
        {
            m_MoveDownButton.visible = m_IndexInList != labelsArray.arraySize - 1;
            m_MoveUpButton.visible = m_IndexInList != 0;
        }
    }
}
