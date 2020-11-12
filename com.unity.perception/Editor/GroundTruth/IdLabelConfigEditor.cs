using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.UIElements;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(IdLabelConfig))]
    class IdLabelConfigEditor : Editor
    {
        private ListView m_LabelListView;
        private ListView m_NonPresentLabelsListView;

        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;

        private VisualElement m_Root;
        private IdLabelConfig m_IdLabelConig;
        private Button m_SaveButton;
        private Button m_AddNewLabelButton;
        private Button m_MoveUpButton;
        private Button m_MoveDownButton;
        private Button m_ImportFromFileButton;

        private List<string> m_AddedLabels = new List<string>();
        private SerializedProperty m_SerializedLabelsArray;

        private static HashSet<string> allLabelsInProject = new HashSet<string>();
        private List<string> m_LabelsNotPresentInConfig = new List<string>();

        public void OnEnable()
        {
            m_UxmlPath = m_UxmlDir + "LabelConfig_Main.uxml";
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree();
            m_LabelListView = m_Root.Q<ListView>("labels-listview");

            m_NonPresentLabelsListView = m_Root.Q<ListView>("labels-in-project-listview");
            m_SaveButton = m_Root.Q<Button>("save-button");
            m_AddNewLabelButton = m_Root.Q<Button>("add-label");
            m_MoveUpButton = m_Root.Q<Button>("move-up-button");
            m_MoveDownButton = m_Root.Q<Button>("move-down-button");
            m_ImportFromFileButton = m_Root.Q<Button>("import-file-button");

            m_SaveButton.SetEnabled(false);
            m_IdLabelConig = (IdLabelConfig) serializedObject.targetObject;
            m_SerializedLabelsArray = serializedObject.FindProperty(IdLabelConfig.labelEntriesFieldName);

            UpdateMoveButtonState();

            if (m_AutoAssign)
            {
                AutoAssignIds();
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            RefreshAddedLabels();
            SetupPresentLabelsListView();

            RefreshLabelsMasterList();
            RefreshNonPresentLabels();
            SetupNonPresentLabelsListView();

            m_AddNewLabelButton.clicked += () => { AddNewLabel(m_SerializedLabelsArray, m_AddedLabels); };

            m_MoveDownButton.clicked += () =>
            {
                var selectedIndex = m_LabelListView.selectedIndex;
                if (selectedIndex > -1 && selectedIndex < m_SerializedLabelsArray.arraySize - 1)
                {
                    var currentProperty =
                        m_SerializedLabelsArray.GetArrayElementAtIndex(selectedIndex)
                            .FindPropertyRelative(nameof(IdLabelEntry.label));
                    var bottomProperty = m_SerializedLabelsArray.GetArrayElementAtIndex(selectedIndex + 1)
                        .FindPropertyRelative(nameof(IdLabelEntry.label));

                    var tmpString = bottomProperty.stringValue;
                    bottomProperty.stringValue = currentProperty.stringValue;
                    currentProperty.stringValue = tmpString;

                    m_LabelListView.SetSelection(selectedIndex + 1);

                    serializedObject.ApplyModifiedProperties();
                    RefreshAddedLabels();
                    m_LabelListView.Refresh();
                    RefreshListViewHeight();
                    //AssetDatabase.SaveAssets();
                }
            };

            m_MoveUpButton.clicked += () =>
            {
                var selectedIndex = m_LabelListView.selectedIndex;
                if (selectedIndex > 0)
                {
                    var currentProperty =
                        m_SerializedLabelsArray.GetArrayElementAtIndex(selectedIndex)
                            .FindPropertyRelative(nameof(IdLabelEntry.label));
                    var topProperty = m_SerializedLabelsArray.GetArrayElementAtIndex(selectedIndex - 1)
                        .FindPropertyRelative(nameof(IdLabelEntry.label));

                    var tmpString = topProperty.stringValue;
                    topProperty.stringValue = currentProperty.stringValue;
                    currentProperty.stringValue = tmpString;

                    m_LabelListView.SetSelection(selectedIndex - 1);

                    serializedObject.ApplyModifiedProperties();
                    RefreshAddedLabels();
                    m_LabelListView.Refresh();
                    RefreshListViewHeight();
                    //AssetDatabase.SaveAssets();
                }
            };

            m_LabelListView.RegisterCallback<ClickEvent>(evt => { UpdateMoveButtonState(); });

            m_ImportFromFileButton.clicked += () =>
            {
                var path = EditorUtility.OpenFilePanel("Import label configuration", "", "");
                if (path.Length != 0)
                {
                    var fileContent = File.ReadAllText(path);
                    var jsonObj = JObject.Parse(fileContent);
                    ImportFromJson(jsonObj);
                }
            };
        }

        public override VisualElement CreateInspectorGUI()
        {
            serializedObject.Update();
            m_IdLabelConig = (IdLabelConfig) serializedObject.targetObject;
            RefreshListDataAndPresenation();
            return m_Root;
        }

        static void RefreshLabelsMasterList()
        {
            allLabelsInProject.Clear();

            var allPrefabPaths = GetAllPrefabsInProject();
            foreach (var path in allPrefabPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var labeling = asset.GetComponent<Labeling>();
                if (labeling)
                {
                    allLabelsInProject.UnionWith(labeling.labels);
                }
            }
        }

        void RefreshNonPresentLabels()
        {
            m_LabelsNotPresentInConfig.Clear();
            m_LabelsNotPresentInConfig.AddRange(allLabelsInProject);
            m_LabelsNotPresentInConfig.RemoveAll(label => m_AddedLabels.Contains(label));
        }

        static List<string> GetAllPrefabsInProject()
        {
            var allPaths = AssetDatabase.GetAllAssetPaths();
            var prefabPaths = new List<string>();
            foreach (var path in allPaths)
            {
                if (path.EndsWith(".prefab"))
                    prefabPaths.Add(path);
            }

            return prefabPaths;
        }

        void UpdateMoveButtonState()
        {
            var selectedIndex = m_LabelListView.selectedIndex;
            m_MoveDownButton.SetEnabled(selectedIndex > -1);
            m_MoveUpButton.SetEnabled(selectedIndex > -1);
        }

        public void RefreshListDataAndPresenation()
        {
            serializedObject.Update();
            RefreshAddedLabels();
            RefreshNonPresentLabels();
            m_NonPresentLabelsListView.Refresh();
            RefreshListViewHeight();
            m_LabelListView.Refresh();
        }

        void ScrollToBottomAndSelectLastItem()
        {
            m_LabelListView.SetSelection(m_LabelListView.itemsSource.Count - 1);
            UpdateMoveButtonState();

            m_Root.schedule.Execute(() => { m_LabelListView.ScrollToItem(-1); })
                .StartingIn(
                    10); //to circumvent the delay in the listview's internal scrollview updating its geometry (when new items are added).
        }

        void RefreshAddedLabels()
        {
            m_AddedLabels.Clear();
            m_AddedLabels.AddRange(m_IdLabelConig.labelEntries.Select(entry => entry.label));
        }

        void SetupPresentLabelsListView()
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

            const int itemHeight = 35;
            m_LabelListView.bindItem = BindItem;
            m_LabelListView.makeItem = MakeItem;
            m_LabelListView.itemHeight = itemHeight;
            m_LabelListView.selectionType = SelectionType.Single;

            m_LabelListView.RegisterCallback<AttachToPanelEvent>(evt => { RefreshListViewHeight(); });
        }

        void SetupNonPresentLabelsListView()
        {
            m_NonPresentLabelsListView.itemsSource = m_LabelsNotPresentInConfig;

            VisualElement MakeItem()
            {
                var element = new NonPresentLabelElement(this, m_SerializedLabelsArray);
                return element;
            }

            void BindItem(VisualElement e, int i)
            {
                if (e is NonPresentLabelElement nonPresentLabel)
                {
                    nonPresentLabel.m_Label.text = m_LabelsNotPresentInConfig[i];
                }
            }

            const int itemHeight = 27;

            m_NonPresentLabelsListView.bindItem = BindItem;
            m_NonPresentLabelsListView.makeItem = MakeItem;
            m_NonPresentLabelsListView.itemHeight = itemHeight;
            m_NonPresentLabelsListView.selectionType = SelectionType.None;
        }

        public void SetSaveButtonEnabled(bool enabled)
        {
            m_SaveButton.SetEnabled(enabled);
        }

        public void RefreshListViewHeight()
        {
            m_LabelListView.style.minHeight =
                Mathf.Clamp(m_LabelListView.itemsSource.Count * m_LabelListView.itemHeight, 300, 600);
        }

        bool m_AutoAssign => serializedObject.FindProperty(nameof(IdLabelConfig.autoAssignIds)).boolValue;

        void AutoAssignIds()
        {
            var serializedProperty = serializedObject.FindProperty(IdLabelConfig.labelEntriesFieldName);
            var size = serializedProperty.arraySize;
            if (size == 0)
                return;

            var startingLabelId =
                (StartingLabelId) serializedObject.FindProperty(nameof(IdLabelConfig.startingLabelId)).enumValueIndex;

            var nextId = startingLabelId == StartingLabelId.One ? 1 : 0;
            for (int i = 0; i < size; i++)
            {
                serializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(IdLabelEntry.id)).intValue =
                    nextId;
                nextId++;
            }
        }

        void AddNewLabel(SerializedProperty serializedArray, List<string> presentLabels)
        {
            AddNewLabel(serializedArray, FindNewLabelValue(presentLabels));
        }

        public void AddNewLabel(SerializedProperty serializedArray, string labelToAdd)
        {
            if (m_AddedLabels.Contains(labelToAdd)) //label has already been added, cannot add again
                return;

            int maxLabel = Int32.MinValue;
            if (serializedArray.arraySize == 0)
                maxLabel = -1;

            for (int i = 0; i < serializedArray.arraySize; i++)
            {
                var item = serializedArray.GetArrayElementAtIndex(i);
                maxLabel = math.max(maxLabel, item.FindPropertyRelative(nameof(IdLabelEntry.id)).intValue);
            }

            AppendLabelEntryWithIdToSerializedArray(serializedArray, labelToAdd, maxLabel + 1);

            serializedObject.ApplyModifiedProperties();
            RefreshListDataAndPresenation();
            ScrollToBottomAndSelectLastItem();
        }

        // Add a list of IdLabelEntry objects to the target object's serialized entries array. This function assumes the labelsToAdd list does not contain any duplicate labels.
        void ImportLabelEntryListIntoSerializedArray(SerializedProperty serializedArray,
            List<IdLabelEntry> labelEntriesToAdd)
        {
            serializedArray.ClearArray();
            labelEntriesToAdd = labelEntriesToAdd.OrderBy(entry => entry.id).ToList();
            foreach (var entry in labelEntriesToAdd)
            {
                AppendLabelEntryWithIdToSerializedArray(serializedArray, entry.label, entry.id);
            }

            serializedObject.ApplyModifiedProperties();
            RefreshListDataAndPresenation();
        }

        void AppendLabelEntryWithIdToSerializedArray(SerializedProperty serializedArray, string label, int id)
        {
            var index = serializedArray.arraySize;
            serializedArray.InsertArrayElementAtIndex(index);
            var element = serializedArray.GetArrayElementAtIndex(index);
            var idProperty = element.FindPropertyRelative(nameof(IdLabelEntry.id));
            idProperty.intValue = id;
            var labelProperty = element.FindPropertyRelative(nameof(IdLabelEntry.label));
            labelProperty.stringValue = label;
        }

        string FindNewLabelValue(List<string> labels)
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

        void ImportFromJson(JObject jsonObj)
        {
            List<IdLabelEntry> importedLabelEntries = new List<IdLabelEntry>();
            JToken type;
            if (jsonObj.TryGetValue("LabelEntryType", out type))
            {
                if (type.Value<string>() == nameof(IdLabelEntry))
                {
                    JToken labelsArrayToken;
                    if (jsonObj.TryGetValue("LabelEntries", out labelsArrayToken))
                    {
                        JArray labelsArray = JArray.Parse(labelsArrayToken.ToString());
                        if (labelsArray != null)
                        {
                            foreach (var labelEntryToken in labelsArray)
                            {
                                if (labelEntryToken is JObject entryObject)
                                {
                                    JToken labelToken;
                                    JToken idToken;
                                    if (entryObject.TryGetValue("Label", out labelToken))
                                    {
                                        if (entryObject.TryGetValue("Id", out idToken))
                                        {
                                            int parsedId;
                                            string idString = idToken.Value<string>();
                                            if (Int32.TryParse(idString, out parsedId))
                                            {
                                                string labelString = labelToken.Value<string>();

                                                if (importedLabelEntries.FindAll(entry => entry.label == labelString)
                                                    .Count > 0)
                                                {
                                                    Debug.LogError("File contains a duplicate Label: " + labelString);
                                                }
                                                else if (importedLabelEntries.FindAll(entry => entry.id == parsedId)
                                                    .Count > 0)
                                                {
                                                    Debug.LogError("File contains a duplicate Label Id: " + parsedId);
                                                }
                                                else
                                                {
                                                    var labelEntry = new IdLabelEntry
                                                    {
                                                        label = labelString,
                                                        id = parsedId
                                                    };
                                                    importedLabelEntries.Add(labelEntry);
                                                }
                                            }
                                            else
                                            {
                                                Debug.LogError("Error reading Id for Label Entry" + labelEntryToken +
                                                               " from file. Please make sure a string value is provided.");
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            Debug.LogError("Error reading Id for Label Entry" + labelEntryToken +
                                                           " from file. Please check the formatting.");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogError("Error reading Label for Label Entry" + labelEntryToken +
                                                       " from file. Please check the formatting.");
                                        return;
                                    }
                                }
                                else
                                {
                                    Debug.LogError("Error reading Label Entry " + labelEntryToken +
                                                   " from file. Please check the formatting.");
                                    return;
                                }
                            }

                            ImportLabelEntryListIntoSerializedArray(m_SerializedLabelsArray, importedLabelEntries);
                        }
                        else
                        {
                            Debug.LogError(
                                "Could not read list of Label Entries from file. Please check the formatting.");
                        }
                    }
                }
                else
                {
                    Debug.LogError("Specified LabelEntryType does not match " + nameof(IdLabelEntry));
                }
            }
            else
            {
                Debug.LogError("LabelEntryType not found.");
            }
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


        public LabelElementInLabelConfig(IdLabelConfigEditor editor, SerializedProperty labelsArray,
            ListView labelsListView)
        {
            m_UxmlPath = m_UxmlDir + "LabelElementInLabelConfig.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree(this);
            m_LabelTextField = this.Q<TextField>("label-value");
            m_RemoveButton = this.Q<Button>("remove-button");
            m_MoveUpButton = this.Q<Button>("move-up-button");
            m_MoveDownButton = this.Q<Button>("move-down-button");
            m_LabelId = this.Q<Label>("label-id-value");

            m_LabelTextField.isDelayed = true;

            m_MoveDownButton.clicked += () =>
            {
                if (m_IndexInList < labelsArray.arraySize - 1)
                {
                    var currentProperty =
                        labelsArray.GetArrayElementAtIndex(m_IndexInList)
                            .FindPropertyRelative(nameof(IdLabelEntry.label));
                    var bottomProperty = labelsArray.GetArrayElementAtIndex(m_IndexInList + 1)
                        .FindPropertyRelative(nameof(IdLabelEntry.label));

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
                        labelsArray.GetArrayElementAtIndex(m_IndexInList)
                            .FindPropertyRelative(nameof(IdLabelEntry.label));
                    var topProperty = labelsArray.GetArrayElementAtIndex(m_IndexInList - 1)
                        .FindPropertyRelative(nameof(IdLabelEntry.label));

                    var tmpString = topProperty.stringValue;
                    topProperty.stringValue = currentProperty.stringValue;
                    currentProperty.stringValue = tmpString;

                    m_IndexInList--;
                    labelsListView.SetSelection(m_IndexInList);
                    UpdateMoveButtonVisibility(labelsArray);

                    editor.serializedObject.ApplyModifiedProperties();

                    editor.RefreshListDataAndPresenation();
                    //AssetDatabase.SaveAssets();
                }
            };

            m_LabelTextField.RegisterValueChangedCallback<string>((cEvent) =>
            {
                labelsArray.GetArrayElementAtIndex(m_IndexInList).FindPropertyRelative(nameof(IdLabelEntry.label))
                    .stringValue = cEvent.newValue;
                if (labelsArray.serializedObject.hasModifiedProperties)
                {
                    //the value change event is called even when the listview recycles its child elements for re-use during scrolling, therefore, we should check to make sure there are modified properties, otherwise we would be doing the refresh for no reason (reduces scrolling performance)
                    labelsArray.serializedObject.ApplyModifiedProperties();
                    editor.RefreshListDataAndPresenation();
                }
            });

            m_RemoveButton.clicked += () =>
            {
                labelsArray.DeleteArrayElementAtIndex(m_IndexInList);
                editor.serializedObject.ApplyModifiedProperties();
                editor.RefreshListDataAndPresenation();
            };
        }

        public void UpdateMoveButtonVisibility(SerializedProperty labelsArray)
        {
            m_MoveDownButton.visible = m_IndexInList != labelsArray.arraySize - 1;
            m_MoveUpButton.visible = m_IndexInList != 0;
        }
    }


    class NonPresentLabelElement : VisualElement
    {
        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;
        private Button m_AddButton;
        public Label m_Label;

        public NonPresentLabelElement(IdLabelConfigEditor editor, SerializedProperty labelsArray)
        {
            m_UxmlPath = m_UxmlDir + "SuggestedLabelElement.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree(this);
            m_Label = this.Q<Label>("label-value");
            m_AddButton = this.Q<Button>("add-button");

            m_AddButton.clicked += () => { editor.AddNewLabel(labelsArray, m_Label.text); };
        }
    }
}
