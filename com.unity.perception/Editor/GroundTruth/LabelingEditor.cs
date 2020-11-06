#define ENABLED
#if ENABLED
using System;
using System.Collections;
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
        private ScrollView m_LabelConfigsScrollView;

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

        private List<Type> m_LabelConfigTypes = new List<Type>();
        private List<ScriptableObject> m_AllLabelConfigsInProject = new List<ScriptableObject>();

        private void OnEnable()
        {
            m_LabelConfigTypes = AddToConfigWindow.FindAllSubTypes(typeof(LabelConfig<>));

            var mySerializedObject = new SerializedObject(serializedObject.targetObjects[0]);
            m_SerializedLabelsArray = mySerializedObject.FindProperty("labels");

            m_Labeling = mySerializedObject.targetObject as Labeling;

            m_UxmlPath = m_UxmlDir + "Labeling_Main.uxml";

            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree();

            m_CurrentLabelsListView = m_Root.Q<ListView>("current-labels-listview");
            m_SuggestLabelsListView_FromName = m_Root.Q<ListView>("suggested-labels-name-listview");
            m_SuggestLabelsListView_FromPath = m_Root.Q<ListView>("suggested-labels-path-listview");
            m_LabelConfigsScrollView = m_Root.Q<ScrollView>("label-configs-scrollview");

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
            RefreshLabelConfigsList();
            SetupListsAndScrollers();
            return m_Root;
        }

        void RefreshLabelConfigsList()
        {
            List<string> labelConfigGuids = new List<string>();
            foreach (var type in m_LabelConfigTypes)
            {
                labelConfigGuids.AddRange(AssetDatabase.FindAssets("t:"+type.Name));
            }

            m_AllLabelConfigsInProject.Clear();
            foreach (var configGuid in labelConfigGuids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(configGuid));
                m_AllLabelConfigsInProject.Add(asset);
            }
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

            //Debug.Log("list update, source list count is:" + m_SuggestedLabelsBasedOnPath.Count);
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

        void SetupListsAndScrollers()
        {
            //Labels that have already been added to the target Labeling component
            SetupCurrentLabelsListView();
            //Labels suggested by the system, which the user can add
            SetupSuggestedLabelsListViews();
            //Add labels from Label Configs present in project
            SetupLabelConfigsScrollView();
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
            m_CurrentLabelsListView.selectionType = SelectionType.None;

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
        void SetupLabelConfigsScrollView()
        {
            m_LabelConfigsScrollView.Clear();
            foreach (var config in m_AllLabelConfigsInProject)
            {
                VisualElement configElement = new LabelConfigElement(this, config);
                m_LabelConfigsScrollView.Add(configElement);
            }
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
                foreach (var targetObject in editor.targets)
                {
                    if (targetObject is Labeling labeling)
                    {
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

    class LabelConfigElement : VisualElement
    {
        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;
        private bool m_Collapsed = true;

        private ListView m_LabelsListView;
        private Label m_ConfigName;
        private VisualElement m_CollapseToggle;
        //private Toggle m_HiddenCollapsedToggle;

        public LabelConfigElement(LabelingEditor editor, ScriptableObject config)
        {
            m_UxmlPath = m_UxmlDir + "ConfigElementForAddingLabelsFrom.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree(this);
            m_LabelsListView = this.Q<ListView>("label-config-contents-listview");
            m_ConfigName = this.Q<Label>("config-name");
            m_ConfigName.text = config.name;
            m_CollapseToggle = this.Q<VisualElement>("collapse-toggle");

            var propertyInfo = config.GetType().GetProperty(IdLabelConfig.publicLabelEntriesFieldName);
            if (propertyInfo != null)
            {
                var objectList = (IEnumerable) propertyInfo.GetValue(config);
                var labelEntryList = objectList.Cast<ILabelEntry>().ToList();
                var labelList = labelEntryList.Select(entry => entry.label).ToList();

                m_LabelsListView.itemsSource = labelList;

                VisualElement MakeItem()
                {
                    var element = new SuggestedLabelElement(editor);
                    element.AddToClassList("label_add_from_config");
                    return element;
                }

                void BindItem(VisualElement e, int i)
                {
                    if (e is SuggestedLabelElement suggestedLabel)
                    {
                        suggestedLabel.m_Label.text = labelList[i];
                    }
                }

                const int itemHeight = 27;

                m_LabelsListView.bindItem = BindItem;
                m_LabelsListView.makeItem = MakeItem;
                m_LabelsListView.itemHeight = itemHeight;
                m_LabelsListView.selectionType = SelectionType.None;
            }

            m_CollapseToggle.RegisterCallback<MouseUpEvent>(evt =>
            {
                m_Collapsed = !m_Collapsed;
                ApplyCollapseState();
            });

            ApplyCollapseState();
        }

        void ApplyCollapseState()
        {
            if (m_Collapsed)
            {
                m_CollapseToggle.AddToClassList("collapsed-toggle-state");
                m_LabelsListView.AddToClassList("collapsed");
            }
            else
            {
                m_CollapseToggle.RemoveFromClassList("collapsed-toggle-state");
                m_LabelsListView.RemoveFromClassList("collapsed");
            }
        }
    }
}
#endif
