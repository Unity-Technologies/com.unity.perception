using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    public struct SemanticSegmentationDefinitionEntry : IMessageProducer
    {
        public SemanticSegmentationDefinitionEntry(string name, Color pixelValue)
        {
            labelName = name;
            this.pixelValue = pixelValue;
        }

        public string labelName { get; set; }
        public Color pixelValue { get; set; }

        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddString("labelName", labelName);
            builder.AddIntArray("pixelValue", MessageBuilderUtils.ToIntVector(pixelValue));
        }
    }
}
