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
        private string[] m_NameSeparators = {".","-", "_"};
        private string[] m_PathSeparators = {"/"};

        private List<string> m_SuggestedLabelsBasedOnName = new List<string>();
        private List<string> m_SuggestedLabelsBasedOnPath = new List<string>();

        private Dictionary<int, int> m_CommonsIndexToLabelsIndex = new Dictionary<int, int>();
        private List<string> m_CommonLabels = new List<string>(); //labels that are common between all selected Labeling objects (for multi editing)

        public List<string> CommonLabels => m_CommonLabels;
        public Dictionary<int, int> CommonsIndexToLabelsIndex => m_CommonsIndexToLabelsIndex;

        private void OnEnable()
        {
            var mySerializedObject = new SerializedObject(serializedObject.targetObjects[0]);
            m_SerializedLabelsArray = mySerializedObject.FindProperty("labels");

            m_Labeling = mySerializedObject.targetObject as Labeling;

            m_UxmlPath = m_UxmlDir + "Labeling_Main.uxml";

            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree();

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
                //RefreshUi();
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
            RefreshSuggestedLabelLists();
            SetupListViews();
            return m_Root;
        }

        public void RemoveAddedLabelsFromSuggestedLists()
        {
            m_SuggestedLabelsBasedOnName.RemoveAll(s => m_CommonLabels.Contains(s));
            m_SuggestedLabelsBasedOnPath.RemoveAll(s => m_CommonLabels.Contains(s));
        }

        public void RefreshSuggestedLabelLists()
        {
            m_SuggestedLabelsBasedOnName.Clear();
            m_SuggestedLabelsBasedOnPath.Clear();

            //based on name
            if (serializedObject.targetObjects.Length == 1)
            {
                string assetName = serializedObject.targetObject.name;
                m_SuggestedLabelsBasedOnName.Add(assetName);
                m_SuggestedLabelsBasedOnName.AddRange(assetName.Split(m_NameSeparators, StringSplitOptions.RemoveEmptyEntries).ToList());
            }

            //based on path
            var prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(m_Labeling.gameObject);
            if (prefabObject)
            {
                string assetPath = AssetDatabase.GetAssetPath(prefabObject);
                var stringList = assetPath.Split(m_PathSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
                stringList.Reverse();
                m_SuggestedLabelsBasedOnPath.AddRange(stringList);
            }

            foreach (var targetObject in targets)
            {
                if (targetObject == target)
                    continue; //we have already taken care of this one above

                prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(((Labeling)targetObject).gameObject);
                if (prefabObject)
                {
                    string assetPath = AssetDatabase.GetAssetPath(prefabObject);
                    var stringList = assetPath.Split(m_PathSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
                    m_SuggestedLabelsBasedOnPath = m_SuggestedLabelsBasedOnPath.Intersect(stringList).ToList();
                }
            }

            RemoveAddedLabelsFromSuggestedLists();

            Debug.Log("list update, source list count is:" + m_SuggestedLabelsBasedOnPath.Count);
        }

        public void RefreshData()
        {
            serializedObject.SetIsDifferentCacheDirty();
            serializedObject.Update();
            var mySerializedObject = new SerializedObject(serializedObject.targetObjects[0]);
            m_SerializedLabelsArray = mySerializedObject.FindProperty("labels");
            RefreshCommonLabels();
            RefreshSuggestedLabelLists();
            SetupSuggestedLabelsListViews();
            SetupCurrentLabelsListView();
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

        //to know which index in the asset
        Dictionary<int, int> CreateCommonLabelsToAssetsLabelsIndex()
        {
            return null;
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
                    addedLabel.m_LabelTextField.value = m_SerializedLabelsArray.GetArrayElementAtIndex(m_CommonsIndexToLabelsIndex[i]).stringValue;
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
            SetupSuggestedLabelsBasedOnFlatList(m_SuggestLabelsListView_FromName, m_SuggestedLabelsBasedOnName);
            SetupSuggestedLabelsBasedOnFlatList(m_SuggestLabelsListView_FromPath, m_SuggestedLabelsBasedOnPath);
            //SetupSuggestedBasedOnNameLabelsListView();
            //SetupSuggestedBasedOnPathLabelsListView();
        }

        void SetupSuggestedLabelsBasedOnFlatList(ListView labelsListView, List<string> stringList)
        {
            labelsListView.itemsSource = stringList;

            VisualElement MakeItem() => new SuggestedLabelElement(this);

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
            labelsListView.selectionType = SelectionType.None;
        }

        // void SetupSuggestedBasedOnPathLabelsListView()
        // {
        //     m_SuggestLabelsListView_FromPath.itemsSource = m_SuggestedLabelsBasedOnPath;
        //
        //     VisualElement MakeItem() => new SuggestedLabelElement(this);
        //
        //     void BindItem(VisualElement e, int i)
        //     {
        //         if (e is SuggestedLabelElement suggestedLabel)
        //         {
        //             Debug.Log("bind, source list count is:" + m_SuggestedLabelsBasedOnPath.Count);
        //             suggestedLabel.m_Label.text = m_SuggestedLabelsBasedOnPath[i];
        //         }
        //     }
        //
        //     const int itemHeight = 32;
        //
        //     m_SuggestLabelsListView_FromPath.bindItem = BindItem;
        //     m_SuggestLabelsListView_FromPath.makeItem = MakeItem;
        //     m_SuggestLabelsListView_FromPath.itemHeight = itemHeight;
        //     m_SuggestLabelsListView_FromPath.selectionType = SelectionType.None;
        // }
        //
        // void SetupSuggestedBasedOnNameLabelsListView()
        // {
        //     VisualElement MakeItem() => new SuggestedLabelElement(this);
        //
        //     void BindItem(VisualElement e, int i)
        //     {
        //         if (e is SuggestedLabelElement suggestedLabel)
        //         {
        //             suggestedLabel.m_Label.text = m_SuggestedLabelsBasedOnName[i];
        //         }
        //     }
        //
        //     const int itemHeight = 32;
        //
        //     m_SuggestLabelsListView_FromName.bindItem = BindItem;
        //     m_SuggestLabelsListView_FromName.makeItem = MakeItem;
        //     m_SuggestLabelsListView_FromName.itemHeight = itemHeight;
        //     m_SuggestLabelsListView_FromName.itemsSource = m_SuggestedLabelsBasedOnName;
        //     m_SuggestLabelsListView_FromName.selectionType = SelectionType.None;
        // }
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
                // List<string> m_CommonLabels = new List<string>();
                //
                // m_CommonLabels.Clear();
                // var firstTarget = editor.targets[0] as Labeling;
                // m_CommonLabels.AddRange(firstTarget.labels);
                //
                // foreach (var obj in editor.targets)
                // {
                //     m_CommonLabels = m_CommonLabels.Intersect(((Labeling) obj).labels).ToList();
                // }


                foreach (var targetObject in editor.targets)
                {
                    if (targetObject is Labeling labeling)
                    {
                        // Dictionary<int, int>  commonsIndexToLabelsIndex = new Dictionary<int, int>();
                        //
                        // for (int i = 0; i < labeling.labels.Count; i++)
                        // {
                        //     string label = labeling.labels[i];
                        //
                        //     for (int j = 0; j < editor.CommonLabels.Count; j++)
                        //     {
                        //         string label2 = editor.CommonLabels[j];
                        //
                        //         if (String.Equals(label, label2) && !commonsIndexToLabelsIndex.ContainsKey(j))
                        //         {
                        //             commonsIndexToLabelsIndex.Add(j, i);
                        //         }
                        //     }
                        // }

                        var indexToModifyInTargetLabelList =
                            labeling.labels.IndexOf(editor.CommonLabels[m_IndexInList]);


                        var serializedLabelingObject2 = new SerializedObject(labeling);
                        var serializedLabelArray2 = serializedLabelingObject2.FindProperty("labels");
                        serializedLabelArray2.GetArrayElementAtIndex(indexToModifyInTargetLabelList).stringValue = cEvent.newValue;
                        serializedLabelingObject2.ApplyModifiedProperties();
                        serializedLabelingObject2.SetIsDifferentCacheDirty();
                    }
                }

                editor.RefreshData();
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
                editor.RefreshData();
                //editor.RefreshUi();
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

        public SuggestedLabelElement(LabelingEditor editor)
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
                        if (labeling.labels.Contains(m_Label.text))
                            continue; //Do not allow duplicate labels in one asset. Duplicate labels have no use and cause other operations (especially mutlt asset editing) to get messed up
                        var serializedLabelingObject2 = new SerializedObject(targetObject);
                        var serializedLabelArray2 = serializedLabelingObject2.FindProperty("labels");
                        serializedLabelArray2.InsertArrayElementAtIndex(serializedLabelArray2.arraySize);
                        serializedLabelArray2.GetArrayElementAtIndex(serializedLabelArray2.arraySize-1).stringValue = m_Label.text;
                        serializedLabelingObject2.ApplyModifiedProperties();
                        serializedLabelingObject2.SetIsDifferentCacheDirty();
                        editor.serializedObject.SetIsDifferentCacheDirty();
                    }
                }
                editor.RefreshData();
                //editor.RefreshUi();
            };
        }
    }
}
#endif
