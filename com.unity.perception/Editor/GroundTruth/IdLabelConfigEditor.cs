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
    class IdLabelConfigEditor : LabelConfigEditor<IdLabelEntry>
    {
        protected override LabelConfig<IdLabelEntry> TargetLabelConfig => (IdLabelConfig) serializedObject.targetObject;

        protected override void OnEnableExtended()
        {
            AutoAssignIdsIfNeeded();
            m_MoveDownButton.clicked += MoveSelectedItemDown;
            m_MoveUpButton.clicked += MoveSelectedItemUp;
        }

        public override void PostRemoveOperations()
        {
            AutoAssignIdsIfNeeded();
        }

        private void MoveSelectedItemUp()
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

                m_LabelListView.SetSelection(selectedIndex - 1);

                serializedObject.ApplyModifiedProperties();
                RefreshAddedLabels();
                m_LabelListView.Refresh();
                RefreshListViewHeight();
                //AssetDatabase.SaveAssets();
            }
        }

        private void MoveSelectedItemDown()
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

                m_LabelListView.SetSelection(selectedIndex + 1);

                serializedObject.ApplyModifiedProperties();
                RefreshAddedLabels();
                m_LabelListView.Refresh();
                RefreshListViewHeight();
                //AssetDatabase.SaveAssets();
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
                    addedLabel.m_IndexInList = i;
                    addedLabel.m_LabelTextField.BindProperty(m_SerializedLabelsArray.GetArrayElementAtIndex(i)
                        .FindPropertyRelative(nameof(IdLabelEntry.label)));
                    addedLabel.m_LabelId.text = TargetLabelConfig.labelEntries[i].id.ToString();
                    addedLabel.UpdateMoveButtonVisibility(m_SerializedLabelsArray);
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

        protected override IdLabelEntry ImportFromJsonExtended(string labelString, JObject labelEntryJObject,
            List<IdLabelEntry> previousEntries, bool preventDuplicateIdentifiers = true)
        {
            bool invalid = false;
            int parsedId = -1;

            if (labelEntryJObject.TryGetValue("Id", out var idToken))
            {
                var idString = idToken.Value<string>();
                if (Int32.TryParse(idString, out parsedId))
                {
                    if (preventDuplicateIdentifiers && previousEntries.FindAll(entry => entry.id == parsedId).Count > 0)
                    {
                        Debug.LogError("File contains a duplicate Label Id: " + parsedId);
                        invalid = true;
                    }
                }
                else
                {
                    Debug.LogError("Error parsing Id for Label Entry" + labelEntryJObject +
                                   " from file. Please make sure a string value is provided in the file and that it is convertible to an integer.");
                    invalid = true;
                }
            }
            else
            {
                Debug.LogError("Error reading the Id field for Label Entry" + labelEntryJObject +
                               " from file. Please check the formatting.");
                invalid = true;
            }

            return new IdLabelEntry
            {
                label = invalid? InvalidLabel : labelString,
                id = parsedId
            };
        }


        protected override void AddLabelIdentifierToJson(SerializedProperty labelEntry, JObject jObj)
        {
            jObj.Add("Id", labelEntry.FindPropertyRelative(nameof(IdLabelEntry.id)).intValue.ToString());
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

        protected override void ImportLabelEntryListIntoSerializedArray(SerializedProperty serializedArray,
            List<IdLabelEntry> labelEntriesToAdd)
        {
            labelEntriesToAdd = labelEntriesToAdd.OrderBy(entry => entry.id).ToList();
            base.ImportLabelEntryListIntoSerializedArray(serializedArray, labelEntriesToAdd);
        }

        private bool AutoAssign => serializedObject.FindProperty(nameof(IdLabelConfig.autoAssignIds)).boolValue;

        private void AutoAssignIds()
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

        private void AutoAssignIdsIfNeeded()
        {
            if (AutoAssign)
            {
                AutoAssignIds();
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }
    }

    internal class IdLabelElementInLabelConfig : LabelElementInLabelConfig<IdLabelEntry>
    {
        protected override string UxmlPath => UxmlDir + "IdLabelElementInLabelConfig.uxml";

        public Label m_LabelId;

        public IdLabelElementInLabelConfig(LabelConfigEditor<IdLabelEntry> editor, SerializedProperty labelsArray) :
            base(editor, labelsArray)
        {
        }

        protected override void InitExtended()
        {
            m_LabelId = this.Q<Label>("label-id-value");
        }
    }
}
