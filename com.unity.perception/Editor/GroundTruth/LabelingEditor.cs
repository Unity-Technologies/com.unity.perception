using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering.UI;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(Labeling))]
    class LabelingEditor : Editor
    {
        class MyBinding : IBinding
        {
            private LabelingEditor m_Editor;

            public MyBinding(LabelingEditor editor)
            {
                m_Editor = editor;
            }

            public void PreUpdate()
            {
            }

            public void Update()
            {
                m_Editor.RefreshUi();
            }

            public void Release()
            {
            }
        }

        private Labeling m_Labeling;
        private SerializedProperty m_SerializedLabelsArray;
        private VisualElement m_Root;
        private BindableElement m_OuterElement;

        private ListView m_CurrentLabelsListView;
        private ListView m_SuggestLabelsListView_FromName;
        private ListView m_SuggestLabelsListView_FromPath;
        private ListView m_SuggestLabelsListView_FromDB;

        private Button m_AddButton;

        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;
        private string[] nameSeparators = {".","-", "_"};
        private string[] pathSeparators = {"/"};

        public List<string> suggestedLabelsBasedOnName = new List<string>();
        public List<string> suggestedLabelsBasedOnPath = new List<string>();

        private void OnEnable()
        {
            m_SerializedLabelsArray = serializedObject.FindProperty("labels");

            m_Labeling = (Labeling) target;

            m_UxmlPath = m_UxmlDir + "Labeling_Main.uxml";

            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree();

            m_OuterElement = m_Root.Q<BindableElement>("outer-container");
            m_OuterElement.binding = new MyBinding(this);
            m_OuterElement.bindingPath = "labels";

            m_CurrentLabelsListView = m_Root.Q<ListView>("current-labels-listview");
            m_SuggestLabelsListView_FromName = m_Root.Q<ListView>("suggested-labels-name-listview");
            m_SuggestLabelsListView_FromPath = m_Root.Q<ListView>("suggested-labels-path-listview");
            m_SuggestLabelsListView_FromDB = m_Root.Q<ListView>("suggested-labels-db-listview");

            m_AddButton = m_Root.Q<Button>("add-label");

            m_AddButton.clicked += () =>
            {
                m_SerializedLabelsArray.InsertArrayElementAtIndex(m_SerializedLabelsArray.arraySize);
                m_SerializedLabelsArray.GetArrayElementAtIndex(m_SerializedLabelsArray.arraySize - 1).stringValue =
                    "<New Label>";
                serializedObject.ApplyModifiedProperties();
                m_CurrentLabelsListView.Refresh();
            };

            SetupListViews();
            UpdateSuggestedLabelLists();
        }

        public override VisualElement CreateInspectorGUI()
        {
            serializedObject.Update();
            return m_Root;
        }

        public void RemoveAddedLabelsFromSuggestedLists()
        {
            suggestedLabelsBasedOnName.RemoveAll(s => m_Labeling.labels.Contains(s));
            suggestedLabelsBasedOnPath.RemoveAll(s => m_Labeling.labels.Contains(s));
        }

        public void UpdateSuggestedLabelLists()
        {
            //based on name
            suggestedLabelsBasedOnName.Clear();
            string assetName = m_Labeling.gameObject.name;
            suggestedLabelsBasedOnName.Add(assetName);
            suggestedLabelsBasedOnName.AddRange(assetName.Split(nameSeparators, StringSplitOptions.RemoveEmptyEntries).ToList());
            RemoveAddedLabelsFromSuggestedLists();

            //based on path
            suggestedLabelsBasedOnPath.Clear();
            var prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(m_Labeling.gameObject);
            if (prefabObject)
            {
                string assetPath = AssetDatabase.GetAssetPath(prefabObject);
                var stringList = assetPath.Split(pathSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
                stringList.Reverse();
                suggestedLabelsBasedOnPath.AddRange(stringList);
                RemoveAddedLabelsFromSuggestedLists();
            }
        }

        public void RefreshUi()
        {
            m_CurrentLabelsListView.Refresh();
            m_SuggestLabelsListView_FromName.Refresh();
            m_SuggestLabelsListView_FromPath.Refresh();
            m_SuggestLabelsListView_FromDB.Refresh();
        }

        void SetupListViews()
        {
            //Labels that have already been added to the target Labeling component
            SetupCurrentLabelsListView();
            //Labels suggested by the system, which the user can add
            SetupSuggestedLabelsListViews();
        }

        void SetupCurrentLabelsListView()
        {
            VisualElement MakeItem() =>
                new AddedLabelEditor(this, m_CurrentLabelsListView, serializedObject, m_SerializedLabelsArray);

            void BindItem(VisualElement e, int i)
            {
                if (e is AddedLabelEditor addedLabel)
                {
                    addedLabel.m_IndexInList = i;
                    addedLabel.m_LabelTextField.BindProperty(m_SerializedLabelsArray.GetArrayElementAtIndex(i));
                }
            }

            const int itemHeight = 35;

            m_CurrentLabelsListView.bindItem = BindItem;
            m_CurrentLabelsListView.makeItem = MakeItem;
            m_CurrentLabelsListView.itemHeight = itemHeight;
            m_CurrentLabelsListView.itemsSource = m_Labeling.labels;

            //m_CurrentLabelsListView.reorderable = true;
            //m_CurrentLabelsListView.selectionType = SelectionType.Multiple;
        }

        void SetupSuggestedLabelsListViews()
        {
            SetupSuggestedLabelsBasedOnFlatList(m_SuggestLabelsListView_FromName, suggestedLabelsBasedOnName);
            SetupSuggestedLabelsBasedOnFlatList(m_SuggestLabelsListView_FromPath, suggestedLabelsBasedOnPath);
            //SetupSuggestedLabelsBasedOnFlatList(m_SuggestLabelsListView_FromDB, );
        }

        void SetupSuggestedLabelsBasedOnFlatList(ListView labelsListView, List<string> stringList)
        {
            VisualElement MakeItem() => new SuggestedLabelElement(this, labelsListView,
                m_CurrentLabelsListView,
                m_SerializedLabelsArray, serializedObject);

            void BindItem(VisualElement e, int i)
            {
                if (e is SuggestedLabelElement suggestedLabel)
                {
                    suggestedLabel.m_Label.text = stringList[i];
                }
            }

            const int itemHeight = 32;

            labelsListView.bindItem = BindItem;
            labelsListView.makeItem = MakeItem;
            labelsListView.itemHeight = itemHeight;
            labelsListView.itemsSource = stringList;
            labelsListView.selectionType = SelectionType.None;
        }
    }


    class AddedLabelEditor : VisualElement
    {
        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;
        private VisualElement m_Root;
        private Button m_RemoveButton;
        private Button m_AddToConfigButton;
        public TextField m_LabelTextField;
        public int m_IndexInList;

        public AddedLabelEditor(LabelingEditor editor, ListView listView, SerializedObject serializedLabelingObject, SerializedProperty labelsArrayProperty)
        {
            m_UxmlPath = m_UxmlDir + "AddedLabelElement.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree(this);
            m_LabelTextField = this.Q<TextField>("label-value");
            m_RemoveButton = this.Q<Button>("remove-button");
            m_AddToConfigButton = this.Q<Button>("add-to-config-button");

            m_AddToConfigButton.clicked += () =>
            {
                AddToConfigWindow.ShowWindow(m_LabelTextField.value);
            };

            m_RemoveButton.clicked += () =>
            {
                labelsArrayProperty.DeleteArrayElementAtIndex(m_IndexInList);
                serializedLabelingObject.ApplyModifiedProperties();
                editor.UpdateSuggestedLabelLists();
                editor.RefreshUi();
                listView.Refresh();
            };
        }
    }

    class SuggestedLabelElement : VisualElement
    {
        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;
        private VisualElement m_Root;
        private Button m_AddButton;
        public Label m_Label;

        public SuggestedLabelElement(LabelingEditor editor, ListView suggestedLabelsListView, ListView currentLabelsListView, SerializedProperty serializedLabelArray, SerializedObject serializedLabelingObject)
        {
            m_UxmlPath = m_UxmlDir + "SuggestedLabelElement.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree(this);
            m_Label = this.Q<Label>("label-value");
            m_AddButton = this.Q<Button>("add-button");

            m_AddButton.clicked += () =>
            {
                serializedLabelArray.InsertArrayElementAtIndex(serializedLabelArray.arraySize);
                serializedLabelArray.GetArrayElementAtIndex(serializedLabelArray.arraySize-1).stringValue = m_Label.text;
                serializedLabelingObject.ApplyModifiedProperties();
                editor.RemoveAddedLabelsFromSuggestedLists();
                editor.RefreshUi();
            };
        }
    }


}
