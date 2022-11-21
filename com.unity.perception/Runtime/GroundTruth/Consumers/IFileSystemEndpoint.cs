namespace UnityEngine.Perception.GroundTruth.Consumers
{
    /// <summary>
    /// An interface defined for endpoints that serialize to the file system.
    /// </summary>
    public interface IFileSystemEndpoint
    {
        /// <summary>
        /// The base path the endpoint will write to
        /// </summary>
        string basePath { get; set; }

        /// <summary>
        /// The runtime directory that the dataset will be written to.
        /// This directory may be different from the <see cref="basePath"/> in cases where the <see cref="basePath"/>
        /// has already been taken before the endpoint has been initialized.
        /// </summary>
        string currentPath { get; }

        /// <summary>
        /// The default path the endpoint will write to
        /// </summary>
        string defaultPath { get; }
    }
}
