using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// A mapping between a semantic segmentation color and string label.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public struct SemanticSegmentationDefinitionEntry : IMessageProducer
    {
        /// <summary>
        /// The name of the label.
        /// </summary>
        public string labelName { get; set; }

        /// <summary>
        /// The label color.
        /// </summary>
        public Color32 pixelValue { get; set; }

        /// <summary>
        /// Constructs a new SemanticSegmentationDefinitionEntry.
        /// </summary>
        /// <param name="name">The label name.</param>
        /// <param name="pixelValue">The label color.</param>
        public SemanticSegmentationDefinitionEntry(string name, Color pixelValue)
        {
            labelName = name;
            this.pixelValue = pixelValue;
        }

        /// <inheritdoc/>
        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddString("labelName", labelName);
            builder.AddIntArray("pixelValue", MessageBuilderUtils.ToIntVector(pixelValue));
        }
    }
}
