using System;
using Unity.Mathematics;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(IdLabelConfig))]
    class IdLabelConfigEditor : LabelConfigEditor<IdLabelEntry>
    {
        protected override void InitUiExtended()
        {
            m_StartingIdEnumField.RegisterValueChangedCallback(evt =>
            {
                var id = (int) ((StartingLabelId) evt.newValue);
                serializedObject.FindProperty(nameof(IdLabelConfig.startingLabelId)).enumValueIndex = id;
                serializedObject.ApplyModifiedProperties();
                AutoAssignIds();
            });

            m_AutoIdToggle.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty(nameof(IdLabelConfig.autoAssignIds)).boolValue = evt.newValue;
                m_StartingIdEnumField.SetEnabled(evt.newValue);
                serializedObject.ApplyModifiedProperties();
                if (!evt.newValue)
                {
                    ChangesHappeningInForeground = true;
                    RefreshListDataAndPresentation();
                    //if evt.newValue is true, the auto assign function will perform the above refresh, so no need to do this twice
                    //refresh is needed because the id textfields of the labels need to be enabled or disabled accordingly
                }
                AutoAssignIdsIfNeeded();
            });

            m_StartingIdEnumField.SetEnabled(AutoAssign);

            AutoAssignIdsIfNeeded();
            m_MoveDownButton.clicked += MoveSelectedItemDown;
            m_MoveUpButton.clicked += MoveSelectedItemUp;
        }

        public override void PostRemoveOperations()
        {
            AutoAssignIdsIfNeeded();
        }

        void MoveSelectedItemUp()
        {
            var selectedIndex = m_LabelListView.selectedIndex;
            if (selectedIndex > 0)
            {
                var currentProperty =
                    m_SerializedLabelsArray.GetArrayElementAtIndex(selectedIndex)
                        .FindPropertyRelative(nameof(ILabelEntry.label));
                var topProperty = m_SerializedLabelsArray.GetArrayElementAtIndex(selectedIndex - 1)
                    .FindPropertyRelative(nameof(ILabelEntry.label));

                var tmpString = topProperty.stringValue;
                topProperty.stringValue = currentProperty.stringValue;
                currentProperty.stringValue = tmpString;

                if (!AutoAssign)
                {
                    var currentIdProperty =
                        m_SerializedLabelsArray.GetArrayElementAtIndex(selectedIndex)
                            .FindPropertyRelative(nameof(IdLabelEntry.id));
                    var topIdProperty = m_SerializedLabelsArray.GetArrayElementAtIndex(selectedIndex - 1)
                        .FindPropertyRelative(nameof(IdLabelEntry.id));

                    var tmpInt = topIdProperty.intValue;
                    topIdProperty.intValue = currentIdProperty.intValue;
                    currentIdProperty.intValue = tmpInt;
                }

                m_LabelListView.selectedIndex = selectedIndex - 1;

                serializedObject.ApplyModifiedProperties();
                RefreshAddedLabels();
                m_LabelListView.Refresh();
                RefreshListViewHeight();
            }
        }

        void MoveSelectedItemDown()
        {
            var selectedIndex = m_LabelListView.selectedIndex;
            if (selectedIndex > -1 && selectedIndex < m_SerializedLabelsArray.arraySize - 1)
            {
                var currentProperty =
                    m_SerializedLabelsArray.GetArrayElementAtIndex(selectedIndex)
                        .FindPropertyRelative(nameof(ILabelEntry.label));
                var bottomProperty = m_SerializedLabelsArray.GetArrayElementAtIndex(selectedIndex + 1)
                    .FindPropertyRelative(nameof(ILabelEntry.label));

                var tmpString = bottomProperty.stringValue;
                bottomProperty.stringValue = currentProperty.stringValue;
                currentProperty.stringValue = tmpString;

                if (!AutoAssign)
                {
                    var currentIdProperty =
                        m_SerializedLabelsArray.GetArrayElementAtIndex(selectedIndex)
                            .FindPropertyRelative(nameof(IdLabelEntry.id));
                    var bottomIdProperty = m_SerializedLabelsArray.GetArrayElementAtIndex(selectedIndex + 1)
                        .FindPropertyRelative(nameof(IdLabelEntry.id));

                    var tmpInt = bottomIdProperty.intValue;
                    bottomIdProperty.intValue = currentIdProperty.intValue;
                    currentIdProperty.intValue = tmpInt;
                }

                m_LabelListView.selectedIndex = selectedIndex + 1;

                serializedObject.ApplyModifiedProperties();
                RefreshAddedLabels();
                m_LabelListView.Refresh();
                RefreshListViewHeight();
            }
        }

        protected override void SetupPresentLabelsListView()
        {
            base.SetupPresentLabelsListView();

            VisualElement MakeItem() =>
                new IdLabelElementInLabelConfig(this, m_SerializedLabelsArray);

            void BindItem(VisualElement e, int i)
            {
                if (e is IdLabelElementInLabelConfig addedLabel)
                {
                    addedLabel.indexInList = i;
                    addedLabel.labelTextField.BindProperty(m_SerializedLabelsArray.GetArrayElementAtIndex(i)
                        .FindPropertyRelative(nameof(IdLabelEntry.label)));
                    addedLabel.labelIdTextField.value = m_SerializedLabelsArray.GetArrayElementAtIndex(i)
                        .FindPropertyRelative(nameof(IdLabelEntry.id)).intValue.ToString();
                }
            }

            m_LabelListView.bindItem = BindItem;
            m_LabelListView.makeItem = MakeItem;
        }

        protected override IdLabelEntry CreateLabelEntryFromLabelString(SerializedProperty serializedArray,
            string labelToAdd)
        {
            int maxLabel = Int32.MinValue;
            if (serializedArray.arraySize == 0)
                maxLabel = -1;

            for (int i = 0; i < serializedArray.arraySize; i++)
            {
                var item = serializedArray.GetArrayElementAtIndex(i);
                maxLabel = math.max(maxLabel, item.FindPropertyRelative(nameof(IdLabelEntry.id)).intValue);
            }

            return new IdLabelEntry
            {
                id = maxLabel + 1,
                label = labelToAdd
            };
        }

        protected override void AppendLabelEntryToSerializedArray(SerializedProperty serializedArray,
            IdLabelEntry labelEntry)
        {
            var index = serializedArray.arraySize;
            serializedArray.InsertArrayElementAtIndex(index);
            var element = serializedArray.GetArrayElementAtIndex(index);
            var idProperty = element.FindPropertyRelative(nameof(IdLabelEntry.id));
            idProperty.intValue = labelEntry.id;
            var labelProperty = element.FindPropertyRelative(nameof(ILabelEntry.label));
            labelProperty.stringValue = labelEntry.label;
        }

        public bool AutoAssign => serializedObject.FindProperty(nameof(IdLabelConfig.autoAssignIds)).boolValue;

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

            serializedObject.ApplyModifiedProperties();
            ChangesHappeningInForeground = true;
            RefreshListDataAndPresentation();
            EditorUtility.SetDirty(target);
        }

        void AutoAssignIdsIfNeeded()
        {
            if (AutoAssign)
            {
                AutoAssignIds();
            }
        }

        public int IndexOfGivenIdInSerializedLabelsArray(int id)
        {
            for (int i = 0; i < m_SerializedLabelsArray.arraySize; i++)
            {
                var element = m_SerializedLabelsArray.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(IdLabelEntry.id));
                if (element.intValue == id)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    class IdLabelElementInLabelConfig : LabelElementInLabelConfig<IdLabelEntry>
    {
        protected override string UxmlPath => k_UxmlDir + "IdLabelElementInLabelConfig.uxml";

        public TextField labelIdTextField;

        public IdLabelElementInLabelConfig(LabelConfigEditor<IdLabelEntry> editor, SerializedProperty labelsArray) :
            base(editor, labelsArray)
        {
        }

        protected override void InitExtended()
        {
            labelIdTextField = this.Q<TextField>("label-id-value");
            labelIdTextField.isDelayed = true;
            labelIdTextField.SetEnabled(!((IdLabelConfigEditor) m_LabelConfigEditor).AutoAssign);
            labelIdTextField.RegisterValueChangedCallback(evt =>
            {
                if(int.TryParse(evt.newValue, out int parsedId))
                {
                    m_LabelsArray.GetArrayElementAtIndex(indexInList).FindPropertyRelative(nameof(IdLabelEntry.id))
                        .intValue = parsedId;
                    if (m_LabelsArray.serializedObject.hasModifiedProperties)
                    {
                        m_LabelsArray.serializedObject.ApplyModifiedProperties();
                        m_LabelConfigEditor.ChangesHappeningInForeground = true;
                        m_LabelConfigEditor.RefreshListDataAndPresentation();
                    }

                    var index = ((IdLabelConfigEditor)m_LabelConfigEditor).IndexOfGivenIdInSerializedLabelsArray(parsedId);

                    if (index != -1 && index != indexInList)
                    {
                        //The listview recycles child visual elements and that causes the RegisterValueChangedCallback event to be called when scrolling.
                        //Therefore, we need to make sure we are not in this code block just because of scrolling, but because the user is actively changing one of the labels.
                        //The index check is for this purpose.

                        Debug.LogWarning("A label with the ID " + evt.newValue + " has already been added to this label configuration.");
                    }
                }
                else
                {
                    Debug.LogError("Provided id is not a valid integer. Please provide integer values.");
                    labelIdTextField.value = evt.previousValue;
                }
            });
        }
    }
}
