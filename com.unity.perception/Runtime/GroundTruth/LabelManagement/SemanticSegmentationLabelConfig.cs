using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.LabelManagement
{
    /// <summary>
    /// A definition for how a <see cref="Labeling"/> should be resolved to a single label and color for semantic segmentation generation.
    /// </summary>
    [CreateAssetMenu(fileName = "SemanticSegmentationLabelConfig", menuName = "Perception/Semantic Segmentation Label Config", order = 1)]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class SemanticSegmentationLabelConfig : LabelConfig<SemanticSegmentationLabelEntry>
    {
        /// <summary>
        /// List of standard color based on which this type of label configuration assigns new colors to added labels.
        /// </summary>
        public static readonly List<Color> s_StandardColors = new List<Color>()
        {
            Color.blue,
            Color.green,
            Color.red,
            Color.white,
            Color.yellow,
            Color.gray
        };


        /// <summary>
        /// The color to use for the sky in semantic segmentation images
        /// </summary>
        public Color skyColor = Color.black;
    }

    /// <summary>
    /// LabelEntry for <see cref="SemanticSegmentationLabelConfig"/>. Maps a label to a color.
    /// </summary>
    [Serializable]
    public struct SemanticSegmentationLabelEntry : ILabelEntry
    {
        string ILabelEntry.label => this.label;
        /// <summary>
        /// The label this entry should match.
        /// </summary>
        public string label;
        /// <summary>
        /// The color to be drawn in the semantic segmentation image
        /// </summary>
        public Color color;
    }
}
