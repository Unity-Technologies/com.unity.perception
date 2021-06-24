using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Toggle = UnityEngine.UIElements.Toggle;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(Labeling)), CanEditMultipleObjects]
    class LabelingEditor : Editor
    {
        VisualElement m_Root;
        VisualElement m_ManualLabelingContainer;
        VisualElement m_AutoLabelingContainer;
        VisualElement m_FromLabelConfigsContainer;
        VisualElement m_SuggestedLabelsContainer;
        VisualElement m_SuggestedOnNamePanel;
        VisualElement m_SuggestedOnPathPanel;
        ListView m_CurrentLabelsListView;
        ListView m_SuggestedLabelsListViewFromName;
        ListView m_SuggestedLabelsListViewFromPath;
        ScrollView m_LabelConfigsScrollView;
        PopupField<string> m_LabelingSchemesPopup;
        Button m_AddButton;
        Button m_AddAutoLabelToConfButton;
        Toggle m_AutoLabelingToggle;
        Label m_CurrentAutoLabel;
        Label m_CurrentAutoLabelTitle;
        Label m_AddManualLabelsTitle;

        Labeling m_Labeling;

        string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        string m_UxmlPath;

        List<string> m_SuggestedLabelsBasedOnName = new List<string>();
        List<string> m_SuggestedLabelsBasedOnPath = new List<string>();

        public List<string> CommonLabels { get; private set; } = new List<string>();

        List<Type> m_LabelConfigTypes;
        readonly List<ScriptableObject> m_AllLabelConfigsInProject = new List<ScriptableObject>();

        readonly List<AssetLabelingScheme> m_LabelingSchemes = new List<AssetLabelingScheme>();

        /// <summary>
        /// List of separator characters used for parsing asset names for auto labeling or label suggestion purposes
        /// </summary>
        public static readonly string[] NameSeparators = {".", "-", "_"};
        /// <summary>
        /// List of separator characters used for parsing asset paths for auto labeling or label suggestion purposes
        /// </summary>
        public static readonly string[] PathSeparators = {"/"};

        void OnEnable()
        {
            m_LabelConfigTypes = AddToConfigWindow.FindAllSubTypes(typeof(LabelConfig<>));

            var mySerializedObject = new SerializedObject(serializedObject.targetObjects[0]);
            m_Labeling = mySerializedObject.targetObject as Labeling;

            m_UxmlPath = m_UxmlDir + "Labeling_Main.uxml";
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree();

            m_CurrentLabelsListView = m_Root.Q<ListView>("current-labels-listview");
            m_SuggestedLabelsListViewFromName = m_Root.Q<ListView>("suggested-labels-name-listview");
            m_SuggestedLabelsListViewFromPath = m_Root.Q<ListView>("suggested-labels-path-listview");
            m_LabelConfigsScrollView = m_Root.Q<ScrollView>("label-configs-scrollview");
            m_SuggestedOnNamePanel = m_Root.Q<VisualElement>("suggested-labels-from-name");
            m_SuggestedOnPathPanel = m_Root.Q<VisualElement>("suggested-labels-from-path");
            m_AddButton = m_Root.Q<Button>("add-label");
            m_CurrentAutoLabel = m_Root.Q<Label>("current-auto-label");
            m_CurrentAutoLabelTitle = m_Root.Q<Label>("current-auto-label-title");
            m_AutoLabelingToggle = m_Root.Q<Toggle>("auto-or-manual-toggle");
            m_ManualLabelingContainer = m_Root.Q<VisualElement>("manual-labeling");
            m_AutoLabelingContainer = m_Root.Q<VisualElement>("automatic-labeling");
            m_FromLabelConfigsContainer = m_Root.Q<VisualElement>("from-label-configs");
            m_SuggestedLabelsContainer = m_Root.Q<VisualElement>("suggested-labels");
            m_AddAutoLabelToConfButton = m_Root.Q<Button>("add-auto-label-to-config");
            m_AddManualLabelsTitle = m_Root.Q<Label>("add-manual-labels-title");
            var dropdownParent = m_Root.Q<VisualElement>("drop-down-parent");

            m_ItIsPossibleToAddMultipleAutoLabelsToConfig = false;
            InitializeLabelingSchemes(dropdownParent);
            AssesAutoLabelingStatus();

            m_FirstItemLabelsArray = serializedObject.FindProperty(nameof(Labeling.labels));

            if (serializedObject.targetObjects.Length > 1)
            {
                var addedTitle = m_Root.Q<Label>("added-labels-title");
                addedTitle.text = "Common Labels of Selected Items";

                m_SuggestedOnNamePanel.style.display = DisplayStyle.None;

                m_AddAutoLabelToConfButton.text = "Add Automatic Labels of All Selected Assets to Config...";
            }
            else
            {
                m_AddAutoLabelToConfButton.text = "Add to Label Config...";
            }

            m_AddAutoLabelToConfButton.clicked += () =>
            {
                AddToConfigWindow.ShowWindow(CreateUnionOfAllLabels().ToList());
            };

            m_AddButton.clicked += () =>
            {
                var labelsUnion = CreateUnionOfAllLabels();
                var newLabel = FindNewLabelValue(labelsUnion);
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
                ChangesHappeningInForeground = true;
                RefreshManualLabelingData();
            };

            m_AutoLabelingToggle.RegisterValueChangedCallback(evt =>
            {
                AutoLabelToggleChanged();
            });

            ChangesHappeningInForeground = true;
            m_Root.schedule.Execute(CheckForModelChanges).Every(30);
        }

        int m_PreviousLabelsArraySize = -1;
        /// <summary>
        /// This boolean is used to signify when changes in the model are triggered directly from the inspector UI by the user.
        /// In these cases, the scheduled model checker does not need to update the UI again.
        /// </summary>
        public bool ChangesHappeningInForeground { get; set; }

        SerializedProperty m_FirstItemLabelsArray;

        void CheckForModelChanges()
        {
            if (ChangesHappeningInForeground)
            {
                ChangesHappeningInForeground = false;
                m_PreviousLabelsArraySize = m_FirstItemLabelsArray.arraySize;
                return;
            }

            if (m_FirstItemLabelsArray.arraySize != m_PreviousLabelsArraySize)
            {
                AssesAutoLabelingStatus();
                RefreshManualLabelingData();
                m_PreviousLabelsArraySize = m_FirstItemLabelsArray.arraySize;
            }
        }

        bool SerializedObjectHasValidLabelingScheme(SerializedObject serObj)
        {
            var schemeName = serObj.FindProperty(nameof(Labeling.autoLabelingSchemeType)).stringValue;
            return IsValidLabelingSchemeName(schemeName);
        }

        bool IsValidLabelingSchemeName(string schemeName)
        {
            return schemeName != string.Empty &&
                   m_LabelingSchemes.FindAll(scheme => scheme.GetType().Name == schemeName).Count > 0;
        }

        bool m_ItIsPossibleToAddMultipleAutoLabelsToConfig;

        void UpdateUiAspects()
        {
            m_ManualLabelingContainer.SetEnabled(!m_AutoLabelingToggle.value);
            m_AutoLabelingContainer.SetEnabled(m_AutoLabelingToggle.value);

            m_AddManualLabelsTitle.style.display = m_AutoLabelingToggle.value ? DisplayStyle.None : DisplayStyle.Flex;
            m_FromLabelConfigsContainer.style.display = m_AutoLabelingToggle.value ? DisplayStyle.None : DisplayStyle.Flex;
            m_SuggestedLabelsContainer.style.display = m_AutoLabelingToggle.value ? DisplayStyle.None : DisplayStyle.Flex;

            m_CurrentLabelsListView.style.minHeight = m_AutoLabelingToggle.value ? 70 : 120;

            if (!m_AutoLabelingToggle.value || serializedObject.targetObjects.Length > 1 ||
                !SerializedObjectHasValidLabelingScheme(new SerializedObject(serializedObject.targetObjects[0])))
            {
                m_CurrentAutoLabel.style.display = DisplayStyle.None;
                m_AddAutoLabelToConfButton.SetEnabled(false);
            }
            else
            {
                m_CurrentAutoLabel.style.display = DisplayStyle.Flex;
                m_AddAutoLabelToConfButton.SetEnabled(true);
            }

            if(m_AutoLabelingToggle.value && serializedObject.targetObjects.Length > 1 && m_ItIsPossibleToAddMultipleAutoLabelsToConfig)
            {
                m_AddAutoLabelToConfButton.SetEnabled(true);
            }


            if (serializedObject.targetObjects.Length == 1)
            {
                m_AutoLabelingToggle.text = "Use Automatic Labeling";
            }
            else
            {
                m_CurrentAutoLabelTitle.text = "Select assets individually to inspect their automatic labels.";
                m_AutoLabelingToggle.text = "Use Automatic Labeling for All Selected Items";
            }
        }

        void UpdateCurrentAutoLabelValue(SerializedObject serObj)
        {
            var array = serObj.FindProperty(nameof(Labeling.labels));
            if (array.arraySize > 0)
            {
                m_CurrentAutoLabel.text = array.GetArrayElementAtIndex(0).stringValue;
            }
        }

        bool AreSelectedAssetsCompatibleWithAutoLabelScheme(AssetLabelingScheme scheme)
        {
            foreach (var asset in serializedObject.targetObjects)
            {
                string label = scheme.GenerateLabel(asset);
                if (label == null)
                {
                    return false;
                }
            }
            return true;
        }

        void InitializeLabelingSchemes(VisualElement parent)
        {
            //this function should be called only once during the lifecycle of the editor element
            AssetLabelingScheme labelingScheme = new AssetNameLabelingScheme();
            if (AreSelectedAssetsCompatibleWithAutoLabelScheme(labelingScheme)) m_LabelingSchemes.Add(labelingScheme);

            labelingScheme = new AssetFileNameLabelingScheme();
            if (AreSelectedAssetsCompatibleWithAutoLabelScheme(labelingScheme)) m_LabelingSchemes.Add(labelingScheme);

            labelingScheme = new CurrentOrParentsFolderNameLabelingScheme();
            if (AreSelectedAssetsCompatibleWithAutoLabelScheme(labelingScheme)) m_LabelingSchemes.Add(labelingScheme);

            var descriptions = m_LabelingSchemes.Select(scheme => scheme.Description).ToList();
            descriptions.Insert(0, "<Select Scheme>");
            m_LabelingSchemesPopup = new PopupField<string>(descriptions, 0) {label = "Labeling Scheme"};
            m_LabelingSchemesPopup.style.marginLeft = 0;
            parent.Add(m_LabelingSchemesPopup);

            m_LabelingSchemesPopup.RegisterValueChangedCallback(evt => AssignAutomaticLabelToSelectedAssets());
        }

        void AutoLabelToggleChanged()
        {
            UpdateUiAspects();

            if (!m_AutoLabelingToggle.value)
            {
                m_ItIsPossibleToAddMultipleAutoLabelsToConfig = false;

                foreach (var targetObj in serializedObject.targetObjects)
                {
                    var serObj = new SerializedObject(targetObj);
                    serObj.FindProperty(nameof(Labeling.useAutoLabeling)).boolValue = false;

                    if (SerializedObjectHasValidLabelingScheme(serObj))
                    {
                        //asset already had a labeling scheme before auto labeling was disabled, which means it has auto label(s) attached. these should be cleared now.
                        serObj.FindProperty(nameof(Labeling.labels)).ClearArray();
                    }

                    serObj.FindProperty(nameof(Labeling.autoLabelingSchemeType)).stringValue = string.Empty;
                    m_LabelingSchemesPopup.index = 0;

                    serObj.ApplyModifiedProperties();
                    serObj.SetIsDifferentCacheDirty();
                }
            }

            ChangesHappeningInForeground = true;
            RefreshManualLabelingData();
        }

        void AssignAutomaticLabelToSelectedAssets()
        {
            //the 0th index of this popup is "<Select Scheme>" and should not do anything
            if (m_LabelingSchemesPopup.index == 0)
            {
                return;
            }

            m_ItIsPossibleToAddMultipleAutoLabelsToConfig = true;

            var labelingScheme = m_LabelingSchemes[m_LabelingSchemesPopup.index - 1];

            foreach (var targetObj in serializedObject.targetObjects)
            {
                var serObj = new SerializedObject(targetObj);
                serObj.FindProperty(nameof(Labeling.useAutoLabeling)).boolValue = true; //only set this flag once the user has actually chosen a scheme, otherwise, we will not touch the flag
                serObj.FindProperty(nameof(Labeling.autoLabelingSchemeType)).stringValue = labelingScheme.GetType().Name;
                var serLabelsArray = serObj.FindProperty(nameof(Labeling.labels));
                serLabelsArray.ClearArray();
                serLabelsArray.InsertArrayElementAtIndex(0);
                var label = labelingScheme.GenerateLabel(targetObj);
                serLabelsArray.GetArrayElementAtIndex(0).stringValue = label;
                if (targetObj == serializedObject.targetObjects[0] && serializedObject.targetObjects.Length == 1)
                {
                    UpdateCurrentAutoLabelValue(serObj);
                }
                serObj.ApplyModifiedProperties();
                serObj.SetIsDifferentCacheDirty();
            }

            UpdateUiAspects();
            ChangesHappeningInForeground = true;
            RefreshManualLabelingData();
        }

        void AssesAutoLabelingStatus()
        {
            var enabledOrNot = true;
            if (serializedObject.targetObjects.Length == 1)
            {
                var serObj = new SerializedObject(serializedObject.targetObjects[0]);
                var enabled = serObj.FindProperty(nameof(Labeling.useAutoLabeling)).boolValue;
                m_AutoLabelingToggle.value = enabled;
                var currentLabelingSchemeName = serObj.FindProperty(nameof(Labeling.autoLabelingSchemeType)).stringValue;
                if (IsValidLabelingSchemeName(currentLabelingSchemeName))
                {
                    m_LabelingSchemesPopup.index =
                        m_LabelingSchemes.FindIndex(scheme => scheme.GetType().Name.ToString() == currentLabelingSchemeName) + 1;
                }
                UpdateCurrentAutoLabelValue(serObj);
            }
            else
            {
                string unifiedLabelingScheme = null;
                var allAssetsUseSameLabelingScheme = true;

                foreach (var targetObj in serializedObject.targetObjects)
                {
                    var serObj = new SerializedObject(targetObj);
                    var enabled = serObj.FindProperty(nameof(Labeling.useAutoLabeling)).boolValue;
                    enabledOrNot &= enabled;

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
                }
                m_AutoLabelingToggle.value = enabledOrNot;

                if (allAssetsUseSameLabelingScheme)
                {
                    //all selected assets have the same scheme recorded in their serialized objects
                    m_LabelingSchemesPopup.index =
                        m_LabelingSchemes.FindIndex(scheme => scheme.GetType().Name.ToString() == unifiedLabelingScheme) + 1;

                    m_ItIsPossibleToAddMultipleAutoLabelsToConfig = enabledOrNot;
                    //if all selected assets have the same scheme recorded in their serialized objects, and they all
                    //have auto labeling enabled, we can now add all auto labels to a config
                }
                else
                {
                    //the selected DO NOT have the same scheme recorded in their serialized objects
                    m_LabelingSchemesPopup.index = 0;
                }
            }

            UpdateUiAspects();
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
            m_Labeling = serializedObject.targetObject as Labeling;
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

        void RemoveAddedLabelsFromSuggestedLists()
        {
            m_SuggestedLabelsBasedOnName.RemoveAll(s => CommonLabels.Contains(s));
            m_SuggestedLabelsBasedOnPath.RemoveAll(s => CommonLabels.Contains(s));
        }

        void RefreshSuggestedLabelLists()
        {
            m_SuggestedLabelsBasedOnName.Clear();
            m_SuggestedLabelsBasedOnPath.Clear();

            //based on name
            if (serializedObject.targetObjects.Length == 1)
            {
                string assetName = serializedObject.targetObject.name;
                var pieces = assetName.Split(NameSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (pieces.Count > 1)
                {
                    //means the asset name was actually split
                    m_SuggestedLabelsBasedOnName.Add(assetName);
                }

                m_SuggestedLabelsBasedOnName.AddRange(pieces);
            }

            //based on path
            string assetPath = GetAssetOrPrefabPath(m_Labeling.gameObject);
            //var prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(m_Labeling.gameObject);
            if (assetPath != null)
            {
                var stringList = assetPath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
                stringList.Reverse();
                m_SuggestedLabelsBasedOnPath.AddRange(stringList);
            }

            foreach (var targetObject in targets)
            {
                if (targetObject == target)
                    continue; //we have already taken care of this one above

                assetPath = GetAssetOrPrefabPath(((Labeling)targetObject).gameObject);
                //prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(((Labeling)targetObject).gameObject);
                if (assetPath != null)
                {
                    var stringList = assetPath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
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
            RefreshCommonLabels();
            RefreshSuggestedLabelLists();
            SetupSuggestedLabelsListViews();
            SetupCurrentLabelsListView();
            UpdateSuggestedPanelVisibility();
        }

        void SetupListsAndScrollers()
        {
            //Labels that have already been added to the target Labeling component
            SetupCurrentLabelsListView();
            //Labels suggested by the system, which the user can add
            SetupSuggestedLabelsListViews();
            //Add labels from Label Configs present in project
            SetupLabelConfigsScrollView();
            UpdateSuggestedPanelVisibility();
        }

        void UpdateSuggestedPanelVisibility()
        {
            m_SuggestedOnNamePanel.style.display = m_SuggestedLabelsBasedOnName.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            m_SuggestedOnPathPanel.style.display = m_SuggestedLabelsBasedOnPath.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            if (m_SuggestedLabelsBasedOnPath.Count == 0 && m_SuggestedLabelsBasedOnName.Count == 0)
            {
                m_SuggestedLabelsContainer.style.display = DisplayStyle.None;
            }
        }


        void RefreshCommonLabels()
        {
            CommonLabels.Clear();
            CommonLabels.AddRange(((Labeling)serializedObject.targetObjects[0]).labels);

            foreach (var obj in serializedObject.targetObjects)
            {
                CommonLabels = CommonLabels.Intersect(((Labeling) obj).labels).ToList();
            }
        }

        void SetupCurrentLabelsListView()
        {
            m_CurrentLabelsListView.itemsSource = CommonLabels;

            VisualElement MakeItem() =>
                new AddedLabelEditor(this, m_CurrentLabelsListView);

            void BindItem(VisualElement e, int i)
            {
                if (e is AddedLabelEditor addedLabel)
                {
                    addedLabel.indexInList = i;
                    addedLabel.labelTextField.value = CommonLabels[i];
                }
            }

            const int itemHeight = 35;

            m_CurrentLabelsListView.bindItem = BindItem;
            m_CurrentLabelsListView.makeItem = MakeItem;
            m_CurrentLabelsListView.itemHeight = itemHeight;

            m_CurrentLabelsListView.itemsSource = CommonLabels;
            m_CurrentLabelsListView.selectionType = SelectionType.None;
        }

        void SetupSuggestedLabelsListViews()
        {
            SetupSuggestedLabelsBasedOnFlatList(m_SuggestedLabelsListViewFromName, m_SuggestedLabelsBasedOnName);
            SetupSuggestedLabelsBasedOnFlatList(m_SuggestedLabelsListViewFromPath, m_SuggestedLabelsBasedOnPath);
        }

        void SetupSuggestedLabelsBasedOnFlatList(ListView labelsListView, List<string> stringList)
        {
            labelsListView.itemsSource = stringList;

            VisualElement MakeItem() => new SuggestedLabelElement(this);

            void BindItem(VisualElement e, int i)
            {
                if (e is SuggestedLabelElement suggestedLabel)
                {
                    suggestedLabel.label.text = stringList[i];
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

        /// <summary>
        /// Get the path of the given asset in the project, or get the path of the given Scene GameObject's source prefab if any
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetAssetOrPrefabPath(UnityEngine.Object obj)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);

            if (assetPath == string.Empty)
            {
                //this indicates that gObj is a scene object and not a prefab directly selected from the Project tab
                var prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                if (prefabObject)
                {
                    assetPath = AssetDatabase.GetAssetPath(prefabObject);
                }
            }

            return assetPath;
        }
    }

    class AddedLabelEditor : VisualElement
    {
        string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";

        public TextField labelTextField;
        public int indexInList;

        public AddedLabelEditor(LabelingEditor editor, ListView listView)
        {
            var uxmlPath = m_UxmlDir + "AddedLabelElement.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath).CloneTree(this);
            labelTextField = this.Q<TextField>("label-value");
            var removeButton = this.Q<Button>("remove-button");
            var addToConfigButton = this.Q<Button>("add-to-config-button");

            labelTextField.isDelayed = true;

            labelTextField.RegisterValueChangedCallback((cEvent) =>
            {
                //Do not let the user define a duplicate label
                if (editor.CommonLabels.Contains(cEvent.newValue) && editor.CommonLabels.IndexOf(cEvent.newValue) != indexInList)
                {
                    //The listview recycles child visual elements and that causes the RegisterValueChangedCallback event to be called when scrolling.
                    //Therefore, we need to make sure we are not in this code block just because of scrolling, but because the user is actively changing one of the labels.
                    //The editor.CommonLabels.IndexOf(cEvent.newValue) != m_IndexInList check is for this purpose.

                    Debug.LogError("A label with the string " + cEvent.newValue + " has already been added to selected objects.");
                    editor.ChangesHappeningInForeground = true;
                    editor.RefreshManualLabelingData();
                    return;
                }

                bool shouldRefresh = false;

                foreach (var targetObject in editor.targets)
                {
                    if (targetObject is Labeling labeling)
                    {
                        var indexToModifyInTargetLabelList =
                            labeling.labels.IndexOf(editor.CommonLabels[indexInList]);


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
                {
                    editor.ChangesHappeningInForeground = true;
                    editor.RefreshManualLabelingData();
                }
            });

            addToConfigButton.clicked += () =>
            {
                AddToConfigWindow.ShowWindow(labelTextField.value);
            };

            removeButton.clicked += () =>
            {
                List<string> commonLabels = new List<string>();

                commonLabels.Clear();
                var firstTarget = editor.targets[0] as Labeling;
                if (firstTarget != null)
                {
                    commonLabels.AddRange(firstTarget.labels);

                    foreach (var obj in editor.targets)
                    {
                        commonLabels = commonLabels.Intersect(((Labeling) obj).labels).ToList();
                    }

                    foreach (var targetObject in editor.targets)
                    {
                        if (targetObject is Labeling labeling)
                        {
                            RemoveLabelFromLabelingSerObj(labeling, commonLabels);
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

                    if (string.Equals(label, label2) && !commonsIndexToLabelsIndex.ContainsKey(j))
                    {
                        commonsIndexToLabelsIndex.Add(j, i);
                    }
                }
            }

            var serializedLabelingObject2 = new SerializedObject(labeling);
            var serializedLabelArray2 = serializedLabelingObject2.FindProperty("labels");
            serializedLabelArray2.DeleteArrayElementAtIndex(commonsIndexToLabelsIndex[indexInList]);
            serializedLabelingObject2.ApplyModifiedProperties();
            serializedLabelingObject2.SetIsDifferentCacheDirty();
        }
    }

    class SuggestedLabelElement : VisualElement
    {
        string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        public Label label;

        public SuggestedLabelElement(LabelingEditor editor)
        {
            var uxmlPath = m_UxmlDir + "SuggestedLabelElement.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath).CloneTree(this);
            label = this.Q<Label>("label-value");
            var addButton = this.Q<Button>("add-button");

            addButton.clicked += () =>
            {
                foreach (var targetObject in editor.serializedObject.targetObjects)
                {
                    if (targetObject is Labeling labeling)
                    {
                        if (labeling.labels.Contains(label.text))
                            continue; //Do not allow duplicate labels in one asset. Duplicate labels have no use and cause other operations (especially mutlt asset editing) to get messed up
                        var serializedLabelingObject2 = new SerializedObject(targetObject);
                        var serializedLabelArray2 = serializedLabelingObject2.FindProperty("labels");
                        serializedLabelArray2.InsertArrayElementAtIndex(serializedLabelArray2.arraySize);
                        serializedLabelArray2.GetArrayElementAtIndex(serializedLabelArray2.arraySize-1).stringValue = label.text;
                        serializedLabelingObject2.ApplyModifiedProperties();
                        serializedLabelingObject2.SetIsDifferentCacheDirty();
                        editor.serializedObject.SetIsDifferentCacheDirty();
                    }
                }
                editor.ChangesHappeningInForeground = true;
                editor.RefreshManualLabelingData();
            };
        }
    }

    class LabelConfigElement : VisualElement
    {
        string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        bool m_Collapsed = true;

        ListView m_LabelsListView;
        VisualElement m_CollapseToggle;

        public LabelConfigElement(LabelingEditor editor, ScriptableObject config)
        {
            var uxmlPath = m_UxmlDir + "ConfigElementForAddingLabelsFrom.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath).CloneTree(this);
            m_LabelsListView = this.Q<ListView>("label-config-contents-listview");
            var openButton = this.Q<Button>("open-config-button");
            var configName = this.Q<Label>("config-name");
            configName.text = config.name;
            m_CollapseToggle = this.Q<VisualElement>("collapse-toggle");

            openButton.clicked += () =>
            {
                Selection.SetActiveObjectWithContext(config, null);
            };

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
                        suggestedLabel.label.text = labelList[i];
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

    /// <summary>
    /// A labeling scheme based on which an automatic label can be produced for a given asset. E.g. based on asset name, asset path, etc.
    /// </summary>
    abstract class AssetLabelingScheme
    {
        /// <summary>
        /// The description of how this scheme generates labels. Used in the dropdown menu in the UI.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Generate a label for the given asset
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public abstract string GenerateLabel(UnityEngine.Object asset);
    }

    /// <summary>
    /// Asset labeling scheme that outputs the given asset's name as its automatic label
    /// </summary>
    class AssetNameLabelingScheme : AssetLabelingScheme
    {
        ///<inheritdoc/>
        public override string Description => "Use asset name";

        ///<inheritdoc/>
        public override string GenerateLabel(UnityEngine.Object asset)
        {
            return asset.name;
        }
    }


    /// <summary>
    /// Asset labeling scheme that outputs the given asset's file name, including extension, as its automatic label
    /// </summary>
    class AssetFileNameLabelingScheme : AssetLabelingScheme
    {
        ///<inheritdoc/>
        public override string Description => "Use file name with extension";

        ///<inheritdoc/>
        public override string GenerateLabel(UnityEngine.Object asset)
        {
            string assetPath = LabelingEditor.GetAssetOrPrefabPath(asset);
            var stringList = assetPath.Split(LabelingEditor.PathSeparators, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
            return stringList.Count > 0 ? stringList.Last() : null;
        }
    }


    /// <summary>
    /// Asset labeling scheme that outputs the given asset's folder name as its automatic label
    /// </summary>
    class CurrentOrParentsFolderNameLabelingScheme : AssetLabelingScheme
    {
        ///<inheritdoc/>
        public override string Description => "Use the asset's folder name";

        ///<inheritdoc/>
        public override string GenerateLabel(UnityEngine.Object asset)
        {
            string assetPath = LabelingEditor.GetAssetOrPrefabPath(asset);
            var stringList = assetPath.Split(LabelingEditor.PathSeparators, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
            return stringList.Count > 1 ? stringList[stringList.Count-2] : null;
        }
    }
}
