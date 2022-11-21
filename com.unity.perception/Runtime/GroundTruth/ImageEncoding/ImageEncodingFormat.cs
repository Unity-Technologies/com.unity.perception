namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Image encoding format identifiers.
    /// </summary>
    public enum ImageEncodingFormat
    {
        /// <summary>
        /// An unencoded and uncompressed "raw" image file format.
        /// NOTE: This format is lossless and therefore can be used, for example, to encode segmentation images.
        /// </summary>
        Raw,

        /// <summary>
        /// The JPEG image format.
        /// NOTE: This image format is lossy. The artifacts introduced by this encoding format are not compatible with
        /// segmentation images.
        /// </summary>
        Jpg,

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
