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

        private List<string> m_AddedLabels = new List<string>();

        protected SerializedProperty m_SerializedLabelsArray;

        private static HashSet<string> allLabelsInProject = new HashSet<string>();
        private List<string> m_LabelsNotPresentInConfig = new List<string>();
        private bool m_UiInitialized;
        private bool m_EditorHasUi;

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
        protected VisualElement m_IdSpecificUi;
        protected EnumField m_StartingIdEnumField;
        protected Toggle m_AutoIdToggle;


        public void OnEnable()
        {
            m_SerializedLabelsArray = serializedObject.FindProperty(IdLabelConfig.labelEntriesFieldName);
            m_UiInitialized = false;
            ChangesHappeningInForeground = true;
            RefreshListDataAndPresentation();
        }

        private int m_PreviousLabelsArraySize = -1;
        /// <summary>
        /// This boolean is used to signify when changes in the model are triggered directly from the inspector UI by the user.
        /// In these cases, the scheduled model checker does not need to update the UI again.
        /// </summary>
        public bool ChangesHappeningInForeground { get; set; }
        private void CheckForModelChanges()
        {
            if (ChangesHappeningInForeground)
            {
                ChangesHappeningInForeground = false;
                m_PreviousLabelsArraySize = m_SerializedLabelsArray.arraySize;
                return;
            }

            if (m_SerializedLabelsArray.arraySize != m_PreviousLabelsArraySize)
            {
                RefreshListDataAndPresentation();
                m_PreviousLabelsArraySize = m_SerializedLabelsArray.arraySize;
            }
        }

        protected abstract void InitUiExtended();

        public abstract void PostRemoveOperations();

        public override VisualElement CreateInspectorGUI()
        {
            if (!m_UiInitialized)
            {
                InitUi();
                m_UiInitialized = true;
            }
            serializedObject.Update();
            RefreshListDataAndPresentation();
            return m_Root;
        }

        private void InitUi()
        {
            m_EditorHasUi = true;

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
            m_AutoIdToggle = m_Root.Q<Toggle>("auto-id-toggle");
            m_IdSpecificUi = m_Root.Q<VisualElement>("id-specific-ui");

            m_SaveButton.SetEnabled(false);

            SetupPresentLabelsListView();
            RefreshLabelsMasterList();
            RefreshNonPresentLabels();
            SetupNonPresentLabelsListView();

            InitUiExtended();
            UpdateMoveButtonState(null);

            m_AddNewLabelButton.clicked += () => { AddNewLabel(m_AddedLabels); };

            m_LabelListView.onSelectionChanged +=  UpdateMoveButtonState;

            m_RemoveAllButton.clicked += () =>
            {
                m_SerializedLabelsArray.ClearArray();
                serializedObject.ApplyModifiedProperties();
                ChangesHappeningInForeground = true;
                RefreshListDataAndPresentation();
            };

            m_AddAllButton.clicked += () =>
            {
                foreach (var label in m_LabelsNotPresentInConfig)
                {
                    AppendLabelEntryToSerializedArray(m_SerializedLabelsArray, CreateLabelEntryFromLabelString(m_SerializedLabelsArray, label));
                }
                serializedObject.ApplyModifiedProperties();
                ChangesHappeningInForeground = true;
                RefreshListDataAndPresentation();
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

            m_Root.schedule.Execute(CheckForModelChanges).Every(30);
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

        private void UpdateMoveButtonState(IEnumerable<object> objectList)
        {
            var selectedIndex = m_LabelListView.selectedIndex;
            m_MoveDownButton.SetEnabled(selectedIndex < m_LabelListView.itemsSource.Count - 1);
            m_MoveUpButton.SetEnabled(selectedIndex > 0);
        }

        public void RefreshListDataAndPresentation()
        {
            serializedObject.Update();
            RefreshAddedLabels();
            if (m_EditorHasUi && m_UiInitialized)
            {
                RefreshNonPresentLabels();
                m_NonPresentLabelsListView.Refresh();
                RefreshListViewHeight();
                m_LabelListView.Refresh();
            }
        }

        private void ScrollToBottomAndSelectLastItem()
        {
            m_LabelListView.selectedIndex = m_LabelListView.itemsSource.Count - 1;
            UpdateMoveButtonState(null);

            m_Root.schedule.Execute(() => { m_LabelListView.ScrollToItem(-1); })
                .StartingIn(
                    10); //to circumvent the delay in the listview's internal scrollview updating its geometry (when new items are added).
        }

        protected void RefreshAddedLabels()
        {
            m_AddedLabels.Clear();
            m_SerializedLabelsArray = serializedObject.FindProperty(IdLabelConfig.labelEntriesFieldName);

            for (int i = 0; i < m_SerializedLabelsArray.arraySize; i++)
            {
                m_AddedLabels.Add(m_SerializedLabelsArray.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ILabelEntry.label)).stringValue);
            }
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
                var element = new NonPresentLabelElement<T>(this);
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

        private void AddNewLabel(List<string> presentLabels)
        {
            AddLabel(FindNewLabelString(presentLabels));
        }

        public void AddLabel(string labelToAdd)
        {
            if (m_AddedLabels.Contains(labelToAdd)) //label has already been added, cannot add again
                return;

            AppendLabelEntryToSerializedArray(m_SerializedLabelsArray, CreateLabelEntryFromLabelString(m_SerializedLabelsArray, labelToAdd));

            serializedObject.ApplyModifiedProperties();
            RefreshListDataAndPresentation();
            if (m_EditorHasUi)
                ScrollToBottomAndSelectLastItem();
        }

        public void RemoveLabel(string labelToRemove)
        {
            var index = IndexOfStringLabelInSerializedLabelsArray(labelToRemove);
            if (index >= 0)
            {
                m_SerializedLabelsArray.DeleteArrayElementAtIndex(index);
            }
            serializedObject.ApplyModifiedProperties();
            RefreshListDataAndPresentation();
            if (m_EditorHasUi)
                ScrollToBottomAndSelectLastItem();
        }

        protected abstract T CreateLabelEntryFromLabelString(SerializedProperty serializedArray, string labelToAdd);

        protected abstract void AppendLabelEntryToSerializedArray(SerializedProperty serializedArray, T labelEntry);

        void ImportFromJson(JObject jsonObj)
        {
            Undo.RegisterCompleteObjectUndo(serializedObject.targetObject, "Import new label config");
            JsonUtility.FromJsonOverwrite(jsonObj.ToString(), serializedObject.targetObject);
            ChangesHappeningInForeground = true;
            RefreshListDataAndPresentation();

        }

        private string ExportToJson()
        {
            return JsonUtility.ToJson(serializedObject.targetObject);
        }

        public int IndexOfStringLabelInSerializedLabelsArray(string label)
        {
            for (int i = 0; i < m_SerializedLabelsArray.arraySize; i++)
            {
                var element = m_SerializedLabelsArray.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ILabelEntry.label));
                if (element.stringValue == label)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    internal abstract class LabelElementInLabelConfig<T> : VisualElement where T : ILabelEntry
    {
        protected const string UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        protected abstract string UxmlPath { get; }

        private Button m_RemoveButton;

        public TextField m_LabelTextField;

        public int m_IndexInList;

        protected SerializedProperty m_LabelsArray;


        protected LabelConfigEditor<T> m_LabelConfigEditor;

        protected LabelElementInLabelConfig(LabelConfigEditor<T> editor, SerializedProperty labelsArray)
        {
            m_LabelConfigEditor = editor;
            m_LabelsArray = labelsArray;

            Init();
        }

        private void Init()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath).CloneTree(this);
            m_LabelTextField = this.Q<TextField>("label-value");
            m_RemoveButton = this.Q<Button>("remove-button");

            m_LabelTextField.isDelayed = true;

            InitExtended();

            m_LabelTextField.RegisterValueChangedCallback((cEvent) =>
            {
                int index = m_LabelConfigEditor.IndexOfStringLabelInSerializedLabelsArray(cEvent.newValue);

                if (index != -1 && index != m_IndexInList)
                {
                    //The listview recycles child visual elements and that causes the RegisterValueChangedCallback event to be called when scrolling.
                    //Therefore, we need to make sure we are not in this code block just because of scrolling, but because the user is actively changing one of the labels.
                    //The index check is for this purpose.

                    Debug.LogError("A label with the string " + cEvent.newValue + " has already been added to this label configuration.");
                    m_LabelsArray.GetArrayElementAtIndex(m_IndexInList).FindPropertyRelative(nameof(ILabelEntry.label))
                        .stringValue = cEvent.previousValue; //since the textfield is bound to this property, it has already changed the property, so we need to revert the proprty.
                    m_LabelsArray.serializedObject.ApplyModifiedProperties();
                    m_LabelConfigEditor.ChangesHappeningInForeground = true;
                    m_LabelConfigEditor.RefreshListDataAndPresentation();
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
                    m_LabelConfigEditor.ChangesHappeningInForeground = true;
                    m_LabelConfigEditor.RefreshListDataAndPresentation();
                }
            });

            m_RemoveButton.clicked += () =>
            {
                m_LabelsArray.DeleteArrayElementAtIndex(m_IndexInList);
                m_LabelConfigEditor.PostRemoveOperations();
                m_LabelConfigEditor.serializedObject.ApplyModifiedProperties();
                m_LabelConfigEditor.ChangesHappeningInForeground = true;
                m_LabelConfigEditor.RefreshListDataAndPresentation();
            };
        }

        protected abstract void InitExtended();
    }

    class NonPresentLabelElement<T> : VisualElement where T : ILabelEntry
    {
        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        public Label m_Label;

        public NonPresentLabelElement(LabelConfigEditor<T> editor)
        {
            var uxmlPath = m_UxmlDir + "SuggestedLabelElement.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath).CloneTree(this);
            m_Label = this.Q<Label>("label-value");
            var addButton = this.Q<Button>("add-button");

            addButton.clicked += () => { editor.AddLabel(m_Label.text); };
        }
    }
}
