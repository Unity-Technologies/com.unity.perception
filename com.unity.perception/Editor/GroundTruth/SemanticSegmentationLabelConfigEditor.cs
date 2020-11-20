using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;
using Random = UnityEngine.Random;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(SemanticSegmentationLabelConfig))]
    class SemanticSegmentationLabelConfigEditor : LabelConfigEditor<SemanticSegmentationLabelEntry>
    {
        protected override LabelConfig<SemanticSegmentationLabelEntry> TargetLabelConfig => (SemanticSegmentationLabelConfig) serializedObject.targetObject;

        protected override void OnEnableExtended()
        {
            m_MoveButtons.style.display = DisplayStyle.None;
            m_StartingIdEnumField.style.display = DisplayStyle.None;
        }

        public override void PostRemoveOperations()
        { }

        protected override void SetupPresentLabelsListView()
        {
            base.SetupPresentLabelsListView();

            VisualElement MakeItem() =>
                new ColoredLabelElementInLabelConfig(this, m_SerializedLabelsArray);

            void BindItem(VisualElement e, int i)
            {
                if (e is ColoredLabelElementInLabelConfig addedLabel)
                {
                    addedLabel.m_IndexInList = i;
                    addedLabel.m_LabelTextField.BindProperty(m_SerializedLabelsArray.GetArrayElementAtIndex(i)
                        .FindPropertyRelative(nameof(SemanticSegmentationLabelEntry.label)));
                    addedLabel.m_ColorField.BindProperty(m_SerializedLabelsArray.GetArrayElementAtIndex(i)
                        .FindPropertyRelative(nameof(SemanticSegmentationLabelEntry.color)));
                    addedLabel.UpdateMoveButtonVisibility(m_SerializedLabelsArray);
                }
            }

            m_LabelListView.bindItem = BindItem;
            m_LabelListView.makeItem = MakeItem;
        }

        protected override SemanticSegmentationLabelEntry CreateLabelEntryFromLabelString(SerializedProperty serializedArray, string labelToAdd)
        {
            var standardColorList = new List<Color>(SemanticSegmentationLabelConfig.s_StandardColors);
            for (int i = 0; i < serializedArray.arraySize; i++)
            {
                var item = serializedArray.GetArrayElementAtIndex(i);
                standardColorList.Remove(item.FindPropertyRelative(nameof(SemanticSegmentationLabelEntry.color)).colorValue);
            }

            Color foundColor;
            if (standardColorList.Any())
                foundColor = standardColorList.First();
            else
                foundColor = Random.ColorHSV(0, 1, .5f, 1, 1, 1);

            return new SemanticSegmentationLabelEntry
            {
                color = foundColor,
                label = labelToAdd
            };
        }

        protected override SemanticSegmentationLabelEntry ImportFromJsonExtended(string labelString, JObject labelEntryJObject,
            List<SemanticSegmentationLabelEntry> previousEntries, bool preventDuplicateIdentifiers = true)
        {
            bool invalid = false;
            Color parsedColor = Color.black;

            if (labelEntryJObject.TryGetValue("Color", out var colorToken))
            {
                var colorString = colorToken.Value<string>();
                if (ColorUtility.TryParseHtmlString(colorString, out parsedColor))
                {
                    if (preventDuplicateIdentifiers && previousEntries.FindAll(entry => entry.color == parsedColor).Count > 0)
                    {
                        Debug.LogError("File contains a duplicate Label Color: " + colorString);
                        invalid = true;
                    }
                }
                else
                {
                    Debug.LogError("Error parsing Color for Label Entry" + labelEntryJObject +
                                   " from file. Please make sure a string value is provided in the file and that it is properly formatted as an HTML color.");
                    invalid = true;
                }
            }
            else
            {
                Debug.LogError("Error reading the Color field for Label Entry" + labelEntryJObject +
                               " from file. Please check the formatting.");
                invalid = true;
            }

            return new SemanticSegmentationLabelEntry
            {
                label = invalid? InvalidLabel : labelString,
                color = parsedColor
            };
        }

        protected override void AddLabelIdentifierToJson(SerializedProperty labelEntry, JObject jObj)
        {
            jObj.Add("Color", "#"+ColorUtility.ToHtmlStringRGBA(
                labelEntry.FindPropertyRelative(nameof(SemanticSegmentationLabelEntry.color)).colorValue));
        }

        protected override void AppendLabelEntryToSerializedArray(SerializedProperty serializedArray, SemanticSegmentationLabelEntry semanticSegmentationLabelEntry)
        {
            var index = serializedArray.arraySize;
            serializedArray.InsertArrayElementAtIndex(index);
            var element = serializedArray.GetArrayElementAtIndex(index);
            var colorProperty = element.FindPropertyRelative(nameof(SemanticSegmentationLabelEntry.color));
            colorProperty.colorValue = semanticSegmentationLabelEntry.color;
            var labelProperty = element.FindPropertyRelative(nameof(ILabelEntry.label));
            labelProperty.stringValue = semanticSegmentationLabelEntry.label;
        }
    }

    internal class ColoredLabelElementInLabelConfig : LabelElementInLabelConfig<SemanticSegmentationLabelEntry>
    {
        protected override string UxmlPath => UxmlDir + "ColoredLabelElementInLabelConfig.uxml";

        public ColorField m_ColorField;

        public ColoredLabelElementInLabelConfig(LabelConfigEditor<SemanticSegmentationLabelEntry> editor, SerializedProperty labelsArray) : base(editor, labelsArray)
        { }

        protected override void InitExtended()
        {
            m_ColorField = this.Q<ColorField>("label-color-value");
        }
    }
}
