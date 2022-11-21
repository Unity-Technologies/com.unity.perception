namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Lossless image file formats supported by Perception
    /// </summary>
    public enum LosslessImageEncodingFormat
    {
        /// <summary>
        /// An unencoded and uncompressed "raw" image file format.
        /// NOTE: This format is lossless and therefore can be used, for example, to encode segmentation images.
        /// </summary>
        Raw,

        /// <summary>
        /// The PNG image format.
        /// NOTE: This format is lossless and therefore can be used, for example, to encode segmentation images.
        /// </summary>
        Png,

        /// <summary>
        /// The EXR image format.
        /// NOTE: This format is lossless and therefore can be used, for example, to encode segmentation images.
        /// </summary>
        Exr
    }
}
