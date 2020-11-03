#define ENABLED
#if ENABLED
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
    [CustomEditor(typeof(Labeling)), CanEditMultipleObjects]
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

        private Dictionary<int, int> m_CommonsIndexToLabelsIndex = new Dictionary<int, int>();
        private List<string> m_CommonLabels = new List<string>(); //labels that are common between all selected Labeling objects (for multi editing)
        private void OnEnable()
        {
            var mySerializedObject = new SerializedObject(serializedObject.targetObjects[0]);
            m_SerializedLabelsArray = mySerializedObject.FindProperty("labels");

            m_Labeling = mySerializedObject.targetObject as Labeling;

            m_UxmlPath = m_UxmlDir + "Labeling_Main.uxml";

            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree();

            //m_OuterElement = m_Root.Q<BindableElement>("outer-container");
            //m_OuterElement.binding = new MyBinding(this);
            //m_OuterElement.bindingPath = "labels";


            m_CurrentLabelsListView = m_Root.Q<ListView>("current-labels-listview");
            m_SuggestLabelsListView_FromName = m_Root.Q<ListView>("suggested-labels-name-listview");
            m_SuggestLabelsListView_FromPath = m_Root.Q<ListView>("suggested-labels-path-listview");
            m_SuggestLabelsListView_FromDB = m_Root.Q<ListView>("suggested-labels-db-listview");

            m_AddButton = m_Root.Q<Button>("add-label");

            if (serializedObject.targetObjects.Length > 1)
            {
                var addedTitle = m_Root.Q<Label>("added-labels-title");
                addedTitle.text = "Common Labels of Selected Items";

                var suggestedOnNamePanel = m_Root.Q<VisualElement>("suggested-labels-from-name");
                suggestedOnNamePanel.RemoveFromHierarchy();
            }

            m_AddButton.clicked += () =>
            {
                var labelsUnion = CreateUnionOfAllLabels();
                string newLabel = FindNewLabelValue(labelsUnion);
                foreach (var targetObject in targets)
                {
                    if (targetObject is Labeling labeling)
                    {
                        var serializedLabelingObject2 = new SerializedObject(labeling);
                        var serializedLabelArray2 = serializedLabelingObject2.FindProperty("labels");
                        serializedLabelArray2.InsertArrayElementAtIndex(serializedLabelArray2.arraySize);
                        serializedLabelArray2.GetArrayElementAtIndex(serializedLabelArray2.arraySize-1).stringValue = newLabel;
                        serializedLabelingObject2.ApplyModifiedProperties();
                        serializedLabelingObject2.SetIsDifferentCacheDirty();
                        serializedObject.SetIsDifferentCacheDirty();
                    }
                }
                RefreshData();
                RefreshUi();
            };
        }

        HashSet<string> CreateUnionOfAllLabels()
        {
            HashSet<String> result = new HashSet<string>();
            foreach (var obj in targets)
            {
                if (obj is Labeling labeling)
                {
                    result.UnionWith(labeling.labels);
                }
            }

            return result;
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

        public override VisualElement CreateInspectorGUI()
        {
            serializedObject.Update();
            var mySerializedObject = new SerializedObject(serializedObject.targetObjects[0]);
            m_Labeling = serializedObject.targetObject as Labeling;
            m_SerializedLabelsArray = mySerializedObject.FindProperty("labels");
            RefreshCommonLabels();
            UpdateSuggestedLabelLists();
            SetupListViews();
            return m_Root;
        }

        public void RemoveAddedLabelsFromSuggestedLists()
        {
            suggestedLabelsBasedOnName.RemoveAll(s => m_CommonLabels.Contains(s));
            suggestedLabelsBasedOnPath.RemoveAll(s => m_CommonLabels.Contains(s));
        }

        public void UpdateSuggestedLabelLists()
        {
            suggestedLabelsBasedOnName.Clear();
            suggestedLabelsBasedOnPath.Clear();

            //based on name
            if (serializedObject.targetObjects.Length == 1)
            {
                string assetName = serializedObject.targetObject.name;
                suggestedLabelsBasedOnName.Add(assetName);
                suggestedLabelsBasedOnName.AddRange(assetName.Split(nameSeparators, StringSplitOptions.RemoveEmptyEntries).ToList());
            }




            //based on path

            var prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(m_Labeling.gameObject);
            if (prefabObject)
            {
                string assetPath = AssetDatabase.GetAssetPath(prefabObject);
                var stringList = assetPath.Split(pathSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
                stringList.Reverse();
                suggestedLabelsBasedOnPath.AddRange(stringList);
            }

            foreach (var targetObject in targets)
            {
                if (targetObject == target)
                    continue; //we have already taken care of this one above

                prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(((Labeling)targetObject).gameObject);
                if (prefabObject)
                {
                    string assetPath = AssetDatabase.GetAssetPath(prefabObject);
                    var stringList = assetPath.Split(pathSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
                    suggestedLabelsBasedOnPath = suggestedLabelsBasedOnPath.Intersect(stringList).ToList();
                }
            }


            RemoveAddedLabelsFromSuggestedLists();
        }

        public void RefreshData()
        {
            serializedObject.SetIsDifferentCacheDirty();
            serializedObject.Update();
            var mySerializedObject = new SerializedObject(serializedObject.targetObjects[0]);
            m_SerializedLabelsArray = mySerializedObject.FindProperty("labels");
            //m_Labeling = serializedObject.targetObject as Labeling;
            RefreshCommonLabels();
            SetupCurrentLabelsListView();
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

        void RefreshCommonLabels()
        {
            m_CommonLabels.Clear();
            m_CommonLabels.AddRange(((Labeling)serializedObject.targetObjects[0]).labels);

            foreach (var obj in serializedObject.targetObjects)
            {
                m_CommonLabels = m_CommonLabels.Intersect(((Labeling) obj).labels).ToList();
            }
            Debug.Log("-----------------------------");
            Debug.Log("common labels count: " + m_CommonLabels.Count);
            m_CommonsIndexToLabelsIndex.Clear();
            for (int i = 0; i < m_Labeling.labels.Count; i++)
            {
                string label = m_Labeling.labels[i];

                for (int j = 0; j < m_CommonLabels.Count; j++)
                {
                    string label2 = m_CommonLabels[j];

                    if (String.Equals(label, label2) && !m_CommonsIndexToLabelsIndex.ContainsKey(j))
                    {
                        m_CommonsIndexToLabelsIndex.Add(j, i);
                    }
                }
            }
            Debug.Log("dict labels count: " + m_CommonsIndexToLabelsIndex.Count);

            // if (m_CommonLabels.Count > 0 && serializedObject.targetObjects.Length > 1)
            // {
            //     foreach (var VARIABLE in m_CommonLabels)
            //     {
            //         Debug.Log(VARIABLE);
            //     }
            // }
        }

        void SetupCurrentLabelsListView()
        {
            m_CurrentLabelsListView.itemsSource = m_CommonLabels;
            var mySerializedObject = new SerializedObject(serializedObject.targetObjects[0]);

            VisualElement MakeItem() =>
                new AddedLabelEditor(this, m_CurrentLabelsListView, mySerializedObject, m_SerializedLabelsArray);

            void BindItem(VisualElement e, int i)
            {
                if (e is AddedLabelEditor addedLabel)
                {
                    addedLabel.m_IndexInList = i;
                    addedLabel.m_LabelTextField.BindProperty(m_SerializedLabelsArray.GetArrayElementAtIndex(m_CommonsIndexToLabelsIndex[i]));
                }
            }

            const int itemHeight = 35;

            m_CurrentLabelsListView.bindItem = BindItem;
            m_CurrentLabelsListView.makeItem = MakeItem;
            m_CurrentLabelsListView.itemHeight = itemHeight;

            m_CurrentLabelsListView.itemsSource = m_CommonLabels;


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
            var mySerializedObject = new SerializedObject(serializedObject.targetObjects[0]);

            VisualElement MakeItem() => new SuggestedLabelElement(this, labelsListView,
                m_CurrentLabelsListView, m_SerializedLabelsArray, mySerializedObject);

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


            m_LabelTextField.isDelayed = true;


            //The listview of added labels in the editor is only bound to the top target object, se we need to apply label modifications to other selected objects too
            m_LabelTextField.RegisterValueChangedCallback<string>((cEvent) =>
            {
                List<string> m_CommonLabels = new List<string>();

                m_CommonLabels.Clear();
                var firstTarget = editor.targets[0] as Labeling;
                m_CommonLabels.AddRange(firstTarget.labels);

                foreach (var obj in editor.targets)
                {
                    m_CommonLabels = m_CommonLabels.Intersect(((Labeling) obj).labels).ToList();
                }


                foreach (var targetObject in editor.targets)
                {
                    if (targetObject != editor.targets[0] && targetObject is Labeling labeling)
                    {
                        Dictionary<int, int>  commonsIndexToLabelsIndex = new Dictionary<int, int>();

                        for (int i = 0; i < labeling.labels.Count; i++)
                        {
                            string label = labeling.labels[i];

                            for (int j = 0; j < m_CommonLabels.Count; j++)
                            {
                                string label2 = m_CommonLabels[j];

                                if (String.Equals(label, label2) && !commonsIndexToLabelsIndex.ContainsKey(j))
                                {
                                    commonsIndexToLabelsIndex.Add(j, i);
                                }
                            }
                        }


                        var serializedLabelingObject2 = new SerializedObject(labeling);
                        var serializedLabelArray2 = serializedLabelingObject2.FindProperty("labels");
                        serializedLabelArray2.GetArrayElementAtIndex(commonsIndexToLabelsIndex[m_IndexInList]).stringValue = cEvent.newValue;
                        serializedLabelingObject2.ApplyModifiedProperties();
                        serializedLabelingObject2.SetIsDifferentCacheDirty();

                    }
                }

            });

            m_AddToConfigButton.clicked += () =>
            {
                AddToConfigWindow.ShowWindow(m_LabelTextField.value);
            };

            m_RemoveButton.clicked += () =>
            {
                // labelsArrayProperty.DeleteArrayElementAtIndex(m_IndexInList);
                // serializedLabelingObject.ApplyModifiedProperties();
                // editor.UpdateSuggestedLabelLists();
                // editor.RefreshUi();
                // listView.Refresh();
                List<string> m_CommonLabels = new List<string>();

                m_CommonLabels.Clear();
                var firstTarget = editor.targets[0] as Labeling;
                m_CommonLabels.AddRange(firstTarget.labels);

                foreach (var obj in editor.targets)
                {
                    m_CommonLabels = m_CommonLabels.Intersect(((Labeling) obj).labels).ToList();
                }

                foreach (var targetObject in editor.targets)
                {
                    if (targetObject is Labeling labeling)
                    {
                        RemoveLabelFromLabelingSerObj(labeling, m_CommonLabels);
                    }
                }
                editor.serializedObject.SetIsDifferentCacheDirty();
                editor.RemoveAddedLabelsFromSuggestedLists();
                editor.RefreshData();
                editor.RefreshUi();
            };
        }

        void RemoveLabelFromLabelingSerObj(Labeling labeling, List<string> commonLabels)
        {
            Dictionary<int, int>  commonsIndexToLabelsIndex = new Dictionary<int, int>();

            for (int i = 0; i < labeling.labels.Count; i++)
            {
                string label = labeling.labels[i];

                for (int j = 0; j < commonLabels.Count; j++)
                {
                    string label2 = commonLabels[j];

                    if (String.Equals(label, label2) && !commonsIndexToLabelsIndex.ContainsKey(j))
                    {
                        commonsIndexToLabelsIndex.Add(j, i);
                    }
                }
            }

            var serializedLabelingObject2 = new SerializedObject(labeling);
            var serializedLabelArray2 = serializedLabelingObject2.FindProperty("labels");
            serializedLabelArray2.DeleteArrayElementAtIndex(commonsIndexToLabelsIndex[m_IndexInList]);
            serializedLabelingObject2.ApplyModifiedProperties();
            serializedLabelingObject2.SetIsDifferentCacheDirty();
        }
    }

    class SuggestedLabelElement : VisualElement
    {
        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;
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
                foreach (var targetObject in editor.serializedObject.targetObjects)
                {
                    if (targetObject is Labeling labeling)
                    {
                        //if (labeling.labels.Contains(m_Label.text))
                        //    continue; //Do not allow duplicate labels in one asset. Duplicate labels have no use and cause other operations (especially mutlt asset editing) to get messed up
                        var serializedLabelingObject2 = new SerializedObject(targetObject);
                        var serializedLabelArray2 = serializedLabelingObject2.FindProperty("labels");
                        serializedLabelArray2.InsertArrayElementAtIndex(serializedLabelArray2.arraySize);
                        serializedLabelArray2.GetArrayElementAtIndex(serializedLabelArray2.arraySize-1).stringValue = m_Label.text;
                        serializedLabelingObject2.ApplyModifiedProperties();
                        serializedLabelingObject2.SetIsDifferentCacheDirty();
                        editor.serializedObject.SetIsDifferentCacheDirty();
                    }
                }
                editor.RemoveAddedLabelsFromSuggestedLists();
                editor.RefreshData();
                editor.RefreshUi();
            };
        }
    }
}
#endif
