namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// An annotation definition describes a particular type of annotation and contains an annotation-specific
    /// specification describing how annotation data should be mapped back to labels or objects in the scene.
    ///
    /// Typically, this specification describes all labels_id and label_name used by the annotation.
    /// Some special cases like semantic segmentation might assign additional values (e.g. pixel value) to record the
    /// mapping between label_id/label_name and pixel color in the annotated PNG files.
    /// </summary>
    public abstract class AnnotationDefinition : DataModelElement
    {
        /// <summary>
        /// The description of the annotation.
        /// </summary>
        public abstract string description { get; }

        /// <summary>
        /// Creates an annotation definition file.
        /// </summary>
        /// <param name="id">The id of the annotation</param>
        protected AnnotationDefinition(string id) : base(id)
        {
        }

        /// <inheritdoc />
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("description", description);
        }

        /// <inheritdoc />
        public override bool IsValid()
        {
            return base.IsValid() && !string.IsNullOrEmpty(description);
        }
    }
}
