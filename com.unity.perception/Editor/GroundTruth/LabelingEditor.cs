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
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UIElements.Toggle;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(Labeling)), CanEditMultipleObjects]
    class LabelingEditor : Editor
    {
        private Labeling m_Labeling;
        private SerializedProperty m_SerializedLabelsArray;
        //private SerializedProperty m_AutoLabelingBoolProperty;
        private VisualElement m_Root;
        private BindableElement m_OuterElement;
        private VisualElement m_ManualLabelingContainer;
        private VisualElement m_AutoLabelingContainer;
        private ListView m_CurrentLabelsListView;
        private ListView m_SuggestLabelsListView_FromName;
        private ListView m_SuggestLabelsListView_FromPath;
        private ScrollView m_LabelConfigsScrollView;
        private PopupField<string> m_LabelingSchemesPopup;
        private Button m_AddButton;
        private Label m_CurrentAutoLabel;
        private Label m_CurrentAutoLabelTitle;
        private Toggle m_AutoLabelingToggle;

        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;

        private List<string> m_SuggestedLabelsBasedOnName = new List<string>();
        private List<string> m_SuggestedLabelsBasedOnPath = new List<string>();

        private List<string> m_CommonLabels = new List<string>(); //labels that are common between all selected Labeling objects (for multi editing)
        public List<string> CommonLabels => m_CommonLabels;

        private List<Type> m_LabelConfigTypes = new List<Type>();
        private List<ScriptableObject> m_AllLabelConfigsInProject = new List<ScriptableObject>();

        private List<AssetLabelingScheme> m_LabelingSchemes = new List<AssetLabelingScheme>();

        private void OnEnable()
        {
            m_LabelConfigTypes = AddToConfigWindow.FindAllSubTypes(typeof(LabelConfig<>));

            var mySerializedObject = new SerializedObject(serializedObject.targetObjects[0]);
            m_SerializedLabelsArray = mySerializedObject.FindProperty("labels");

            //m_AutoLabelingBoolProperty = serializedObject.FindProperty(nameof(Labeling.useAutoLabeling));

            m_Labeling = mySerializedObject.targetObject as Labeling;

            m_UxmlPath = m_UxmlDir + "Labeling_Main.uxml";

            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree();

            m_CurrentLabelsListView = m_Root.Q<ListView>("current-labels-listview");
            m_SuggestLabelsListView_FromName = m_Root.Q<ListView>("suggested-labels-name-listview");
            m_SuggestLabelsListView_FromPath = m_Root.Q<ListView>("suggested-labels-path-listview");
            m_LabelConfigsScrollView = m_Root.Q<ScrollView>("label-configs-scrollview");
            m_AddButton = m_Root.Q<Button>("add-label");
            m_CurrentAutoLabel = m_Root.Q<Label>("current-auto-label");
            m_CurrentAutoLabelTitle = m_Root.Q<Label>("current-auto-label-title");
            m_AutoLabelingToggle = m_Root.Q<Toggle>("auto-or-manual-toggle");
            m_ManualLabelingContainer = m_Root.Q<VisualElement>("manual-labeling");
            m_AutoLabelingContainer = m_Root.Q<VisualElement>("automatic-labeling");

            var dropdownParent = m_Root.Q<VisualElement>("drop-down-parent");
            InitializeLabelingSchemes(dropdownParent);

            AssesAutoLabelingStatus();

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
                        var serializedLabelArray2 = serializedLabelingObject2.FindProperty(nameof(Labeling.labels));
                        serializedLabelArray2.InsertArrayElementAtIndex(serializedLabelArray2.arraySize);
                        serializedLabelArray2.GetArrayElementAtIndex(serializedLabelArray2.arraySize-1).stringValue = newLabel;
                        serializedLabelingObject2.ApplyModifiedProperties();
                        serializedLabelingObject2.SetIsDifferentCacheDirty();
                        serializedObject.SetIsDifferentCacheDirty();
                    }
                }
                RefreshManualLabelingData();
            };

            m_AutoLabelingToggle.RegisterValueChangedCallback<bool>(evt =>
            {
                AutoLabelToggleChanged();
            });
        }

        void UpdateActivenessOfUiElements()
        {
            m_ManualLabelingContainer.SetEnabled(!m_AutoLabelingToggle.value);
            m_AutoLabelingContainer.SetEnabled(m_AutoLabelingToggle.value);
            if (serializedObject.targetObjects.Length > 1)
            {
                m_CurrentAutoLabelTitle.text = "Select assets individually to inspect their automatic labels.";
                m_CurrentAutoLabel.style.display = DisplayStyle.None;
            }
            // if (m_LabelingSchemesPopup.index == 0)
            // {
            //     m_CurrentAutoLabel.style.display = DisplayStyle.None;
            //
            // }
        }

        void InitializeLabelingSchemes(VisualElement parent)
        {
            //this function should be called only once during the lifecycle of the editor element
            m_LabelingSchemes.Add(new AssetNameLabelingScheme());
            m_LabelingSchemes.Add(new AssetFileNameLabelingScheme());
            m_LabelingSchemes.Add(new CurrentOrParentsFolderNameLabelingScheme());

            var descriptions = m_LabelingSchemes.Select(scheme => scheme.Description).ToList();
            descriptions.Insert(0, "<Select Scheme>");
            m_LabelingSchemesPopup = new PopupField<string>(descriptions, 0) {label = "Labeling Scheme"};
            m_LabelingSchemesPopup.style.marginLeft = 0;
            parent.Add(m_LabelingSchemesPopup);

            m_LabelingSchemesPopup.RegisterValueChangedCallback<string>(evt => AssignAutomaticLabelToSelectedAssets());
        }

        void AutoLabelToggleChanged()
        {
            UpdateActivenessOfUiElements();

            if (m_AutoLabelingToggle.value)
            {
                if (serializedObject.targetObjects.Length == 1)
                {
                    SyncAutoLabelWithSchemeSingleTarget(true);
                }
            }
            else
            {
                foreach (var targetObj in serializedObject.targetObjects)
                {
                    var serObj = new SerializedObject(targetObj);
                    serObj.FindProperty(nameof(Labeling.useAutoLabeling)).boolValue = false;

                    var schemeName = serObj.FindProperty(nameof(Labeling.autoLabelingSchemeType)).stringValue;
                    if (schemeName != String.Empty)
                    {
                        //asset already had a labeling scheme before auto labeling was disabled, which means it has auto label(s) attached. these should be cleared now.
                        serObj.FindProperty(nameof(Labeling.labels)).ClearArray();
                    }

                    serObj.ApplyModifiedProperties();
                    serObj.SetIsDifferentCacheDirty();
                }
            }

            RefreshManualLabelingData();
        }

        void AssignAutomaticLabelToSelectedAssets()
        {
            UpdateActivenessOfUiElements();
            //the 0th index of this popup is "<Select Scheme>" and should not do anything

            if (m_LabelingSchemesPopup.index == 0)
            {
                return;
            }

            var labelingScheme = m_LabelingSchemes[m_LabelingSchemesPopup.index - 1];
            string topAssetAutoLabel = String.Empty;

            foreach (var targetObj in serializedObject.targetObjects)
            {
                var serObj = new SerializedObject(targetObj);
                serObj.FindProperty(nameof(Labeling.useAutoLabeling)).boolValue = true; //only set this flag once the user has actually chosen a scheme, otherwise, we will not touch the flag
                serObj.FindProperty(nameof(Labeling.autoLabelingSchemeType)).stringValue = labelingScheme.GetType().Name;

                BackupManualLabels(serObj);

                var serLabelsArray = serObj.FindProperty(nameof(Labeling.labels));
                serLabelsArray.ClearArray();
                serLabelsArray.InsertArrayElementAtIndex(0);
                var label = labelingScheme.GenerateLabel(targetObj);
                serLabelsArray.GetArrayElementAtIndex(0).stringValue = label;
                if (targetObj == serializedObject.targetObjects[0] && serializedObject.targetObjects.Length == 1)
                {
                    topAssetAutoLabel = label;
                }
                serObj.ApplyModifiedProperties();
                serObj.SetIsDifferentCacheDirty();
            }

            m_CurrentAutoLabel.text = topAssetAutoLabel;
        }

        void AssignAutomaticLabelToSerializedObject(SerializedObject serObj, string labelingSchemeName)
        {
            var labelingScheme = m_LabelingSchemes.Find(scheme => scheme.GetType().Name == labelingSchemeName);
            AssignAutomaticLabelToSerializedObject(serObj, labelingScheme);
        }

        void AssignAutomaticLabelToSerializedObject(SerializedObject serObj, AssetLabelingScheme labelingScheme)
        {
            var serLabelsArray = serObj.FindProperty(nameof(Labeling.labels));
            var label = labelingScheme.GenerateLabel(serObj.targetObject);
            BackupManualLabels(serObj);
            serLabelsArray.ClearArray();
            serLabelsArray.InsertArrayElementAtIndex(0);
            serLabelsArray.GetArrayElementAtIndex(0).stringValue = label;
            serObj.ApplyModifiedProperties();
            serObj.SetIsDifferentCacheDirty();
        }

        void BackupManualLabels(SerializedObject serObj)
        {
            // var serLabelsArray = serObj.FindProperty(nameof(Labeling.labels));
            // var backupLabelsArray = serObj.FindProperty(nameof(Labeling.manualLabelsBackup));
            // backupLabelsArray.ClearArray();
            // for (int i = 0; i < serLabelsArray.arraySize; i++)
            // {
            //     backupLabelsArray.InsertArrayElementAtIndex(i);
            //     backupLabelsArray.GetArrayElementAtIndex(i).stringValue =
            //         serLabelsArray.GetArrayElementAtIndex(i).stringValue;
            // }
        }

        void RetrieveFromManualLabelsBackupArray(SerializedObject serObj)
        {
            // var serLabelsArray = serObj.FindProperty(nameof(Labeling.labels));
            // var backupLabelsArray = serObj.FindProperty(nameof(Labeling.manualLabelsBackup));
            // serLabelsArray.ClearArray();
            // for (int i = 0; i < backupLabelsArray.arraySize; i++)
            // {
            //     serLabelsArray.InsertArrayElementAtIndex(i);
            //     serLabelsArray.GetArrayElementAtIndex(i).stringValue =
            //         backupLabelsArray.GetArrayElementAtIndex(i).stringValue;
            // }
        }

        void SyncAutoLabelWithSchemeSingleTarget(bool enableAutoLabeling = false)
        {
            var serObj = new SerializedObject(serializedObject.targetObjects[0]);
            var currentLabelingSchemeName = serObj.FindProperty(nameof(Labeling.autoLabelingSchemeType)).stringValue;
            if (currentLabelingSchemeName != String.Empty)
            {
                if (enableAutoLabeling)
                {
                    //the useAutoLabeling flag is only turned on for an asset if a valid scheme for auto labeling has also been chosen, that is why it is deferred to here instead of immediately on toggle click
                    serObj.FindProperty(nameof(Labeling.useAutoLabeling)).boolValue = true;
                }
                AssignAutomaticLabelToSerializedObject(serObj, currentLabelingSchemeName);
                var labelsArray = serObj.FindProperty(nameof(Labeling.labels));
                if (labelsArray.arraySize > 0)
                {
                    var autoLabel = labelsArray.GetArrayElementAtIndex(0).stringValue;
                    m_CurrentAutoLabel.text = autoLabel;
                }

                m_LabelingSchemesPopup.index =
                    m_LabelingSchemes.FindIndex(scheme => scheme.GetType().Name.ToString() == currentLabelingSchemeName) + 1;
            }
            else
            {

            }
        }

        void AssesAutoLabelingStatus()
        {
            var enabledOrNot = true;
            if (serializedObject.targetObjects.Length == 1)
            {
                m_AutoLabelingToggle.text = "Use Automatic Labeling";
                m_CurrentAutoLabel.style.display = DisplayStyle.Flex;

                var serObj = new SerializedObject(serializedObject.targetObjects[0]);
                var enabled = serObj.FindProperty(nameof(Labeling.useAutoLabeling)).boolValue;
                SyncAutoLabelWithSchemeSingleTarget();
                m_AutoLabelingToggle.value = enabled;
            }
            else
            {
                string unifiedLabelingScheme = null;
                bool allAssetsUseSameLabelingScheme = true;
                m_AutoLabelingToggle.text = "Use Automatic Labeling for All Selected Items";
                m_CurrentAutoLabel.style.display = DisplayStyle.None;

                foreach (var targetObj in serializedObject.targetObjects)
                {
                    var serObj = new SerializedObject(targetObj);
                    var enabled = serObj.FindProperty(nameof(Labeling.useAutoLabeling)).boolValue;
                    enabledOrNot &= enabled;
                    // if (enabled)
                    // {
                    var schemeName = serObj.FindProperty(nameof(Labeling.autoLabelingSchemeType)).stringValue;
                    if (schemeName == string.Empty)
                    {
                        //if any of the selected assets does not have a labeling scheme, they can't all have the same valid scheme
                        allAssetsUseSameLabelingScheme = false;
                    }

                    if (allAssetsUseSameLabelingScheme)
                    {
                        if (unifiedLabelingScheme == null)
                        {
                            unifiedLabelingScheme = schemeName;
                        }
                        else if (unifiedLabelingScheme != schemeName)
                        {
                            allAssetsUseSameLabelingScheme = false;
                        }
                    }

                    if (targetObj == serializedObject.targetObjects[0])
                    {
                        var labelsArray = serObj.FindProperty(nameof(Labeling.labels));
                        if (labelsArray.arraySize > 0)
                        {
                            var autoLabelOfTopSelectedItem = labelsArray.GetArrayElementAtIndex(0).stringValue;
                            m_CurrentAutoLabel.text = autoLabelOfTopSelectedItem;
                        }
                    }
                    //}
                }
                m_AutoLabelingToggle.value = enabledOrNot;

                if (allAssetsUseSameLabelingScheme)
                {
                    //all selected assets are using auto labeling, all using the same scheme
                    m_LabelingSchemesPopup.index =
                        m_LabelingSchemes.FindIndex(scheme => scheme.GetType().Name.ToString() == unifiedLabelingScheme) + 1;
                }
                else
                {
                    //the selected assets are all using auto labeling, but not using the same scheme
                    m_LabelingSchemesPopup.index = 0;
                }
            }

            if (!enabledOrNot)
                m_CurrentAutoLabel.text = String.Empty;

            UpdateActivenessOfUiElements();
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
                m_SuggestedLabelsBasedOnName.AddRange(assetName.Split(Labeling.NameSeparators, StringSplitOptions.RemoveEmptyEntries).ToList());
            }

            //based on path
            string assetPath = Labeling.GetAssetOrPrefabPath(m_Labeling.gameObject);
            //var prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(m_Labeling.gameObject);
            if (assetPath != null)
            {
                var stringList = assetPath.Split(Labeling.PathSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
                stringList.Reverse();
                m_SuggestedLabelsBasedOnPath.AddRange(stringList);
            }

            foreach (var targetObject in targets)
            {
                if (targetObject == target)
                    continue; //we have already taken care of this one above

                assetPath = Labeling.GetAssetOrPrefabPath(((Labeling)targetObject).gameObject);
                //prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(((Labeling)targetObject).gameObject);
                if (assetPath != null)
                {
                    var stringList = assetPath.Split(Labeling.PathSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
                    m_SuggestedLabelsBasedOnPath = m_SuggestedLabelsBasedOnPath.Intersect(stringList).ToList();
                }
            }

            RemoveAddedLabelsFromSuggestedLists();
            //Debug.Log("list update, source list count is:" + m_SuggestedLabelsBasedOnPath.Count);
        }

        public void RefreshManualLabelingData()
        {
            serializedObject.SetIsDifferentCacheDirty();
            serializedObject.Update();
            var mySerializedObject = new SerializedObject(serializedObject.targetObjects[0]);
            m_SerializedLabelsArray = mySerializedObject.FindProperty(nameof(Labeling.labels));
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
                    addedLabel.m_LabelTextField.value = m_CommonLabels[i];
                }
            }

            const int itemHeight = 35;

            m_CurrentLabelsListView.bindItem = BindItem;
            m_CurrentLabelsListView.makeItem = MakeItem;
            m_CurrentLabelsListView.itemHeight = itemHeight;

            m_CurrentLabelsListView.itemsSource = m_CommonLabels;
            m_CurrentLabelsListView.selectionType = SelectionType.None;
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


    internal class AddedLabelEditor : VisualElement
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

            ScrollView tmp = listView.Q<ScrollView>();

            tmp.verticalScroller.slider.RegisterCallback<MouseDownEvent>(evt =>
            {
                Debug.Log("mouse down");
            });

            listView.RegisterCallback<FocusInEvent>(evt =>
            {
                Debug.Log("list focused");
            });


            m_LabelTextField.RegisterCallback<FocusOutEvent>(evt =>
            {
                Debug.Log("focus out");
            });

            m_LabelTextField.RegisterCallback<FocusInEvent>(evt =>
            {
                Debug.Log("focus in");
            });

            m_LabelTextField.RegisterValueChangedCallback<string>((cEvent) =>
            {
                //Do not let the user define a duplicate label
                if (editor.CommonLabels.Contains(cEvent.newValue) && editor.CommonLabels.IndexOf(cEvent.newValue) != m_IndexInList)
                {
                    //The listview recycles child visual elements and that causes the RegisterValueChangedCallback event to be called when scrolling.
                    //Therefore, we need to make sure we are not in this code block just because of scrolling, but because the user is actively changing one of the labels.
                    //The editor.CommonLabels.IndexOf(cEvent.newValue) != m_IndexInList check is for this purpose.

                    Debug.LogError("A label with the string " + cEvent.newValue + " has already been added to selected objects.");
                    editor.RefreshManualLabelingData();
                    return;
                }

                bool shouldRefresh = false;

                foreach (var targetObject in editor.targets)
                {
                    if (targetObject is Labeling labeling)
                    {
                        var indexToModifyInTargetLabelList =
                            labeling.labels.IndexOf(editor.CommonLabels[m_IndexInList]);


                        var serializedLabelingObject2 = new SerializedObject(labeling);
                        var serializedLabelArray2 = serializedLabelingObject2.FindProperty(nameof(Labeling.labels));
                        serializedLabelArray2.GetArrayElementAtIndex(indexToModifyInTargetLabelList).stringValue = cEvent.newValue;
                        shouldRefresh = shouldRefresh || serializedLabelArray2.serializedObject.hasModifiedProperties;
                        serializedLabelingObject2.ApplyModifiedProperties();
                        serializedLabelingObject2.SetIsDifferentCacheDirty();
                    }
                }

                //the value change event is called even when the listview recycles its child elements for re-use during scrolling, therefore, we should check to make sure there are modified properties, otherwise we would be doing the refresh for no reason (reduces scrolling performance)
                if (shouldRefresh)
                    editor.RefreshManualLabelingData();
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
                if (firstTarget)
                {
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
                    editor.RefreshManualLabelingData();
                }
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

    internal class SuggestedLabelElement : VisualElement
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
                editor.RefreshManualLabelingData();
                //editor.RefreshUi();
            };
        }
    }

    internal class LabelConfigElement : VisualElement
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
