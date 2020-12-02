using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Perception.GroundTruth
{
    abstract class LabelConfigEditor<T> : Editor where T : ILabelEntry
    {
        string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        string m_UxmlPath;

        int m_AddedLabelsItemHeight = 37;
        int m_OtherLabelsItemHeight = 27;

        List<string> m_AddedLabels = new List<string>();

        protected SerializedProperty m_SerializedLabelsArray;

        HashSet<string> m_AllLabelsInProject = new HashSet<string>();
        List<string> m_LabelsNotPresentInConfig = new List<string>();
        bool m_UiInitialized;
        bool m_EditorHasUi;

        VisualElement m_Root;

        Button m_SaveButton;
        Button m_AddNewLabelButton;
        Button m_RemoveAllButton;
        Button m_AddAllButton;
        Button m_ImportFromFileButton;
        Button m_ExportToFileButton;
        ListView m_NonPresentLabelsListView;

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

        int m_PreviousLabelsArraySize = -1;
        /// <summary>
        /// This boolean is used to signify when changes in the model are triggered directly from the inspector UI by the user.
        /// In these cases, the scheduled model checker does not need to update the UI again.
        /// </summary>
        public bool ChangesHappeningInForeground { get; set; }

        void CheckForModelChanges()
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

        void InitUi()
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

#if UNITY_2020_1_OR_NEWER
            m_LabelListView.onSelectionChange +=  UpdateMoveButtonState;
#else
            m_LabelListView.onSelectionChanged +=  UpdateMoveButtonState;
#endif
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

        void RefreshLabelsMasterList()
        {
            m_AllLabelsInProject.Clear();

            var allPrefabPaths = GetAllPrefabsInProject();
            foreach (var path in allPrefabPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var labeling = asset.GetComponent<Labeling>();
                if (labeling)
                {
                    m_AllLabelsInProject.UnionWith(labeling.labels);
                }
            }
        }

        void RefreshNonPresentLabels()
        {
            m_LabelsNotPresentInConfig.Clear();
            m_LabelsNotPresentInConfig.AddRange(m_AllLabelsInProject);
            m_LabelsNotPresentInConfig.RemoveAll(label => m_AddedLabels.Contains(label));
        }

        static IEnumerable<string> GetAllPrefabsInProject()
        {
            var allPaths = AssetDatabase.GetAllAssetPaths();
            return allPaths.Where(path => path.EndsWith(".prefab")).ToList();
        }

        void UpdateMoveButtonState(IEnumerable<object> objectList)
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

        void ScrollToBottomAndSelectLastItem()
        {
            m_LabelListView.selectedIndex = m_LabelListView.itemsSource.Count - 1;
            UpdateMoveButtonState(null);

            m_Root.schedule.Execute(() => { m_LabelListView.ScrollToItem(-1); })
                .StartingIn(
                    10); //to circumvent the delay in listview's internal scrollview updating its geometry (when new items are added).
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

        void SetupNonPresentLabelsListView()
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
                    nonPresentLabel.label.text = m_LabelsNotPresentInConfig[i];
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

        void AddNewLabel(List<string> presentLabels)
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

        string ExportToJson()
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

    abstract class LabelElementInLabelConfig<T> : VisualElement where T : ILabelEntry
    {
        protected const string k_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        protected abstract string UxmlPath { get; }

        Button m_RemoveButton;

        public TextField labelTextField;

        public int indexInList;

        protected SerializedProperty m_LabelsArray;

        protected LabelConfigEditor<T> m_LabelConfigEditor;

        protected LabelElementInLabelConfig(LabelConfigEditor<T> editor, SerializedProperty labelsArray)
        {
            m_LabelConfigEditor = editor;
            m_LabelsArray = labelsArray;

            Init();
        }

        void Init()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath).CloneTree(this);
            labelTextField = this.Q<TextField>("label-value");
            m_RemoveButton = this.Q<Button>("remove-button");

            labelTextField.isDelayed = true;

            InitExtended();

            labelTextField.RegisterValueChangedCallback((cEvent) =>
            {
                int index = m_LabelConfigEditor.IndexOfStringLabelInSerializedLabelsArray(cEvent.newValue);

                if (index != -1 && index != indexInList)
                {
                    //The listview recycles child visual elements and that causes the RegisterValueChangedCallback event to be called when scrolling.
                    //Therefore, we need to make sure we are not in this code block just because of scrolling, but because the user is actively changing one of the labels.
                    //The index check is for this purpose.

                    Debug.LogError("A label with the string " + cEvent.newValue + " has already been added to this label configuration.");
                    m_LabelsArray.GetArrayElementAtIndex(indexInList).FindPropertyRelative(nameof(ILabelEntry.label))
                        .stringValue = cEvent.previousValue; //since the textfield is bound to this property, it has already changed the property, so we need to revert the property.
                    m_LabelsArray.serializedObject.ApplyModifiedProperties();
                    m_LabelConfigEditor.ChangesHappeningInForeground = true;
                    m_LabelConfigEditor.RefreshListDataAndPresentation();
                    return;
                }


                //even though the textfield is already bound to the relevant property, we need to explicitly set the
                //property here too in order to make "hasModifiedProperties" return the right value in the next line. Otherwise it will always be false.
                m_LabelsArray.GetArrayElementAtIndex(indexInList).FindPropertyRelative(nameof(ILabelEntry.label))
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
                m_LabelsArray.DeleteArrayElementAtIndex(indexInList);
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
        string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        public Label label;

        public NonPresentLabelElement(LabelConfigEditor<T> editor)
        {
            var uxmlPath = m_UxmlDir + "SuggestedLabelElement.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath).CloneTree(this);
            label = this.Q<Label>("label-value");
            var addButton = this.Q<Button>("add-button");

            addButton.clicked += () => { editor.AddLabel(label.text); };
        }
    }
}
