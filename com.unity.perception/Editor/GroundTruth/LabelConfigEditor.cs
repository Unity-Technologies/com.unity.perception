using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;
using UnityEditor.UIElements;

namespace UnityEditor.Perception.GroundTruth
{
    abstract class LabelConfigEditor<T> : Editor where T : ILabelEntry
    {
        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;

        private int m_AddedLabelsItemHeight = 37;
        private int m_OtherLabelsItemHeight = 27;

        protected abstract LabelConfig<T> TargetLabelConfig { get; }

        private List<string> m_AddedLabels = new List<string>();
        public List<string> AddedLabels => m_AddedLabels;

        protected SerializedProperty m_SerializedLabelsArray;

        private static HashSet<string> allLabelsInProject = new HashSet<string>();
        private List<string> m_LabelsNotPresentInConfig = new List<string>();

        private VisualElement m_Root;

        private Button m_SaveButton;
        private Button m_AddNewLabelButton;
        private Button m_RemoveAllButton;
        private Button m_AddAllButton;
        private Button m_ImportFromFileButton;
        private Button m_ExportToFileButton;
        private ListView m_NonPresentLabelsListView;

        protected ListView m_LabelListView;
        protected Button m_MoveUpButton;
        protected Button m_MoveDownButton;
        protected VisualElement m_MoveButtons;
        protected EnumField m_StartingIdEnumField;


        public void OnEnable()
        {
            m_UxmlPath = m_UxmlDir + "LabelConfig_Main.uxml";
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree();
            m_LabelListView = m_Root.Q<ListView>("labels-listview");

            m_NonPresentLabelsListView = m_Root.Q<ListView>("labels-in-project-listview");
            m_SaveButton = m_Root.Q<Button>("save-button");
            m_AddNewLabelButton = m_Root.Q<Button>("add-label");
            m_RemoveAllButton = m_Root.Q<Button>("remove-all-labels");
            m_MoveUpButton = m_Root.Q<Button>("move-up-button");
            m_MoveDownButton = m_Root.Q<Button>("move-down-button");
            m_MoveButtons = m_Root.Q<VisualElement>("move-buttons");
            m_ImportFromFileButton = m_Root.Q<Button>("import-file-button");
            m_ExportToFileButton = m_Root.Q<Button>("export-file-button");
            m_AddAllButton = m_Root.Q<Button>("add-all-labels-in-project");
            m_StartingIdEnumField = m_Root.Q<EnumField>("starting-id-dropdown");

            m_SaveButton.SetEnabled(false);
            m_SerializedLabelsArray = serializedObject.FindProperty(IdLabelConfig.labelEntriesFieldName);

            UpdateMoveButtonState();

            RefreshAddedLabels();
            SetupPresentLabelsListView();

            RefreshLabelsMasterList();
            RefreshNonPresentLabels();
            SetupNonPresentLabelsListView();

            OnEnableExtended();

            m_AddNewLabelButton.clicked += () => { AddNewLabel(m_SerializedLabelsArray, m_AddedLabels); };

            m_RemoveAllButton.clicked += () =>
            {
                m_SerializedLabelsArray.ClearArray();
                serializedObject.ApplyModifiedProperties();
                RefreshListDataAndPresenation();
            };

            m_AddAllButton.clicked += () =>
            {
                foreach (var label in m_LabelsNotPresentInConfig)
                {
                    AppendLabelEntryToSerializedArray(m_SerializedLabelsArray, CreateLabelEntryFromLabelString(m_SerializedLabelsArray, label));
                }
                serializedObject.ApplyModifiedProperties();
                RefreshListDataAndPresenation();
            };

            m_ImportFromFileButton.clicked += () =>
            {
                var path = EditorUtility.OpenFilePanel("Import label configuration from file", "", "json");
                if (path.Length != 0)
                {
                    var fileContent = File.ReadAllText(path);
                    var jsonObj = JObject.Parse(fileContent);
                    ImportFromJson(jsonObj);
                }
            };

            m_ExportToFileButton.clicked += () =>
            {
                var path = EditorUtility.SaveFilePanel("Export label configuration to file", "", this.name, "json");
                if (path.Length != 0)
                {
                    string fileContents = ExportToJson();
                    var writer = File.CreateText(path);

                    writer.Write(fileContents);
                    writer.Flush();
                    writer.Close();
                }
            };
        }


        protected abstract void OnEnableExtended();

        public abstract void PostRemoveOperations();

        public override VisualElement CreateInspectorGUI()
        {
            serializedObject.Update();
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

        private void RefreshNonPresentLabels()
        {
            m_LabelsNotPresentInConfig.Clear();
            m_LabelsNotPresentInConfig.AddRange(allLabelsInProject);
            m_LabelsNotPresentInConfig.RemoveAll(label => m_AddedLabels.Contains(label));
        }

        private static IEnumerable<string> GetAllPrefabsInProject()
        {
            var allPaths = AssetDatabase.GetAllAssetPaths();
            return allPaths.Where(path => path.EndsWith(".prefab")).ToList();
        }

        private void UpdateMoveButtonState()
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

        private void ScrollToBottomAndSelectLastItem()
        {
            m_LabelListView.selectedIndex = m_LabelListView.itemsSource.Count - 1;
            UpdateMoveButtonState();

            m_Root.schedule.Execute(() => { m_LabelListView.ScrollToItem(-1); })
                .StartingIn(
                    10); //to circumvent the delay in the listview's internal scrollview updating its geometry (when new items are added).
        }

        protected void RefreshAddedLabels()
        {
            m_AddedLabels.Clear();
            m_AddedLabels.AddRange(TargetLabelConfig.labelEntries.Select(entry => entry.label));
        }

        protected virtual void SetupPresentLabelsListView()
        {
            m_LabelListView.itemsSource = m_AddedLabels;
            m_LabelListView.itemHeight = m_AddedLabelsItemHeight;
            m_LabelListView.selectionType = SelectionType.Single;

            m_LabelListView.RegisterCallback<AttachToPanelEvent>(evt => { RefreshListViewHeight(); });
        }

        private void SetupNonPresentLabelsListView()
        {
            m_NonPresentLabelsListView.itemsSource = m_LabelsNotPresentInConfig;

            VisualElement MakeItem()
            {
                var element = new NonPresentLabelElement<T>(this, m_SerializedLabelsArray);
                return element;
            }

            void BindItem(VisualElement e, int i)
            {
                if (e is NonPresentLabelElement<T> nonPresentLabel)
                {
                    nonPresentLabel.m_Label.text = m_LabelsNotPresentInConfig[i];
                }
            }

            m_NonPresentLabelsListView.bindItem = BindItem;
            m_NonPresentLabelsListView.makeItem = MakeItem;
            m_NonPresentLabelsListView.itemHeight = m_OtherLabelsItemHeight;
            m_NonPresentLabelsListView.selectionType = SelectionType.None;
        }

        // public void SetSaveButtonEnabled(bool enabled)
        // {
        //     m_SaveButton.SetEnabled(enabled);
        // }

        protected void RefreshListViewHeight()
        {
            m_LabelListView.style.minHeight =
                Mathf.Clamp(m_LabelListView.itemsSource.Count * m_LabelListView.itemHeight, 300, 600);
        }

        string FindNewLabelString(List<string> labels)
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

        private void AddNewLabel(SerializedProperty serializedArray, List<string> presentLabels)
        {
            AddLabel(serializedArray, FindNewLabelString(presentLabels));
        }

        public void AddLabel(SerializedProperty serializedArray, string labelToAdd)
        {
            if (m_AddedLabels.Contains(labelToAdd)) //label has already been added, cannot add again
                return;

            AppendLabelEntryToSerializedArray(serializedArray, CreateLabelEntryFromLabelString(serializedArray, labelToAdd));

            serializedObject.ApplyModifiedProperties();
            RefreshListDataAndPresenation();
            ScrollToBottomAndSelectLastItem();
        }

        protected abstract T CreateLabelEntryFromLabelString(SerializedProperty serializedArray, string labelToAdd);


        // Import a list of IdLabelEntry objects to the target object's serialized entries array. This function assumes the labelsToAdd list does not contain any duplicate labels.
        protected virtual void ImportLabelEntryListIntoSerializedArray(SerializedProperty serializedArray,
            List<T> labelEntriesToAdd)
        {
            serializedArray.ClearArray();

            foreach (var entry in labelEntriesToAdd)
            {
                AppendLabelEntryToSerializedArray(serializedArray, entry);
            }

            serializedObject.ApplyModifiedProperties();
            RefreshListDataAndPresenation();
        }
        protected abstract void AppendLabelEntryToSerializedArray(SerializedProperty serializedArray, T labelEntry);


        protected abstract T ImportFromJsonExtended(string labelString, JObject jsonObject, List<T> previousEntries, bool preventDuplicateIdentifiers = true);

        protected const string InvalidLabel = "INVALID_LABEL";
        void ImportFromJson(JObject jsonObj)
        {
            var importedLabelEntries = new List<T>();
            if (jsonObj.TryGetValue("LabelEntryType", out var type))
            {
                if (type.Value<string>() == typeof(T).Name)
                {
                    if (jsonObj.TryGetValue("LabelEntries", out var labelsArrayToken))
                    {
                        JArray labelsArray = JArray.Parse(labelsArrayToken.ToString());
                        if (labelsArray != null)
                        {
                            foreach (var labelEntryToken in labelsArray)
                            {
                                if (labelEntryToken is JObject entryObject)
                                {
                                    if (entryObject.TryGetValue("Label", out var labelToken))
                                    {
                                        string labelString = labelToken.Value<string>();

                                        if (importedLabelEntries.FindAll(entry => entry.label == labelString)
                                            .Count > 0)
                                        {
                                            Debug.LogError("File contains a duplicate Label: " + labelString);
                                            return;
                                        }

                                        T labelEntry = ImportFromJsonExtended(labelString, entryObject, importedLabelEntries);

                                        if (labelEntry.label == InvalidLabel)
                                            return;

                                        importedLabelEntries.Add(labelEntry);
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
                    Debug.LogError("Specified LabelEntryType does not match " + typeof(T).Name);
                }
            }
            else
            {
                Debug.LogError("LabelEntryType not found.");
            }
        }


        protected abstract void AddLabelIdentifierToJson(SerializedProperty labelEntry, JObject jObj);

        private string ExportToJson()
        {
            JObject result = new JObject();
            result.Add("LabelEntryType", typeof(T).Name);

            JArray labelEntries = new JArray();
            for (int i = 0; i < m_SerializedLabelsArray.arraySize; i++)
            {
                var entry = m_SerializedLabelsArray.GetArrayElementAtIndex(i);
                JObject entryJobj = new JObject();
                entryJobj.Add("Label", entry.FindPropertyRelative(nameof(ILabelEntry.label)).stringValue);
                AddLabelIdentifierToJson(entry, entryJobj);
                labelEntries.Add(entryJobj);
            }

            result.Add("LabelEntries", labelEntries);

            return result.ToString();
        }
    }

    internal abstract class LabelElementInLabelConfig<T> : VisualElement where T : ILabelEntry
    {
        protected const string UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        protected abstract string UxmlPath { get; }

        private Button m_RemoveButton;
        private Button m_MoveUpButton;
        private Button m_MoveDownButton;

        public TextField m_LabelTextField;

        public int m_IndexInList;

        protected SerializedProperty m_LabelsArray;

        //protected ListView m_LabelsListView;
        protected LabelConfigEditor<T> m_LabelConfigEditor;

        protected LabelElementInLabelConfig(LabelConfigEditor<T> editor, SerializedProperty labelsArray)
        {
            m_LabelConfigEditor = editor;
            m_LabelsArray = labelsArray;
            //m_LabelsListView = labelsListView;

            Init();
        }

        private void Init()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath).CloneTree(this);
            m_LabelTextField = this.Q<TextField>("label-value");
            m_RemoveButton = this.Q<Button>("remove-button");
            m_MoveUpButton = this.Q<Button>("move-up-button");
            m_MoveDownButton = this.Q<Button>("move-down-button");

            m_LabelTextField.isDelayed = true;

            InitExtended();

            m_LabelTextField.RegisterValueChangedCallback((cEvent) =>
            {
                if (m_LabelConfigEditor.AddedLabels.Contains(cEvent.newValue) && m_LabelConfigEditor.AddedLabels.IndexOf(cEvent.newValue) != m_IndexInList)
                {
                    //The listview recycles child visual elements and that causes the RegisterValueChangedCallback event to be called when scrolling.
                    //Therefore, we need to make sure we are not in this code block just because of scrolling, but because the user is actively changing one of the labels.
                    //The m_LabelConfigEditor.AddedLabels.IndexOf(cEvent.newValue) != m_IndexInList check is for this purpose.

                    Debug.LogError("A label with the string " + cEvent.newValue + " has already been added to this label configuration.");
                    m_LabelsArray.GetArrayElementAtIndex(m_IndexInList).FindPropertyRelative(nameof(ILabelEntry.label))
                        .stringValue = cEvent.previousValue; //since the textfield is bound to this property, it has already changed the property, so we need to revert the proprty.
                    m_LabelsArray.serializedObject.ApplyModifiedProperties();
                    m_LabelConfigEditor.RefreshListDataAndPresenation();
                    return;
                }


                //even though the textfield is already bound to the relevant property, we need to explicitly set the
                //property here too in order to make "hasModifiedProperties" return the right value in the next line. Otherwise it will always be false.
                m_LabelsArray.GetArrayElementAtIndex(m_IndexInList).FindPropertyRelative(nameof(ILabelEntry.label))
                    .stringValue = cEvent.newValue;
                if (m_LabelsArray.serializedObject.hasModifiedProperties)
                {
                    //the value change event is called even when the listview recycles its child elements for re-use during scrolling, therefore, we should check to make sure there are modified properties, otherwise we would be doing the refresh for no reason (reduces scrolling performance)
                    m_LabelsArray.serializedObject.ApplyModifiedProperties();
                    m_LabelConfigEditor.RefreshListDataAndPresenation();
                }
            });

            m_RemoveButton.clicked += () =>
            {
                m_LabelsArray.DeleteArrayElementAtIndex(m_IndexInList);
                m_LabelConfigEditor.PostRemoveOperations();
                m_LabelConfigEditor.serializedObject.ApplyModifiedProperties();
                m_LabelConfigEditor.RefreshListDataAndPresenation();
            };
        }

        protected abstract void InitExtended();

        public void UpdateMoveButtonVisibility(SerializedProperty labelsArray)
        {
            m_MoveDownButton.visible = m_IndexInList != labelsArray.arraySize - 1;
            m_MoveUpButton.visible = m_IndexInList != 0;
        }
    }

    class NonPresentLabelElement<T> : VisualElement where T : ILabelEntry
    {
        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        public Label m_Label;

        public NonPresentLabelElement(LabelConfigEditor<T> editor, SerializedProperty labelsArray)
        {
            var uxmlPath = m_UxmlDir + "SuggestedLabelElement.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath).CloneTree(this);
            m_Label = this.Q<Label>("label-value");
            var addButton = this.Q<Button>("add-button");

            addButton.clicked += () => { editor.AddLabel(labelsArray, m_Label.text); };
        }
    }
}
