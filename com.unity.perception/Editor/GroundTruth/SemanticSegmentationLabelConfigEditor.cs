using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(SemanticSegmentationLabelConfig))]
    class SemanticSegmentationLabelConfigEditor : LabelConfigEditor<SemanticSegmentationLabelEntry>
    {
        protected override void InitUiExtended()
        {
            m_MoveButtons.style.display = DisplayStyle.None;
            m_IdSpecificUi.style.display = DisplayStyle.None;
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

        public int IndexOfGivenColorInSerializedLabelsArray(Color color)
        {
            for (int i = 0; i < m_SerializedLabelsArray.arraySize; i++)
            {
                var element = m_SerializedLabelsArray.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(SemanticSegmentationLabelEntry.color));
                if (element.colorValue == color)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    internal class ColoredLabelElementInLabelConfig : LabelElementInLabelConfig<SemanticSegmentationLabelEntry>
    {
        protected override string UxmlPath => UxmlDir + "ColoredLabelElementInLabelConfig.uxml";

        public ColorField m_ColorField;

        public ColoredLabelElementInLabelConfig(LabelConfigEditor<SemanticSegmentationLabelEntry> editor, SerializedProperty labelsArray) : base(editor, labelsArray)
        { }

        private Color previousColor;

        protected override void InitExtended()
        {
            m_ColorField = this.Q<ColorField>("label-color-value");

            m_ColorField.RegisterValueChangedCallback((cEvent) =>
            {
                int index = ((SemanticSegmentationLabelConfigEditor)m_LabelConfigEditor).IndexOfGivenColorInSerializedLabelsArray(cEvent.newValue);

                if (index != -1 && index != m_IndexInList)
                {
                    //The listview recycles child visual elements and that causes the RegisterValueChangedCallback event to be called when scrolling.
                    //Therefore, we need to make sure we are not in this code block just because of scrolling, but because the user is actively changing one of the labels.
                    //The index check is for this purpose.

                    Debug.LogWarning("A label with the chosen color " + cEvent.newValue + " has already been added to this label configuration.");
                }
            });

        }
    }
}
