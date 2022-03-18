using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEngine.Perception.GroundTruth.Consumers
{
    public static class PathUtils
    {
        /// <summary>
        /// Replaces windows slashes "\" with universal paths "/"
        /// </summary>
        /// <param name="path">The path to update</param>
        /// <returns>The cleaned up path</returns>
        public static string EnsurePathsAreUniversal(string path)
        {
            return path.Replace(@"\", "/");
        }

        /// <summary>
        /// Combines paths together. This method always uses the alternative directory spacer,
        /// '/' on windows, mac, and linux machines so that paths writtent to json are consistent.
        /// </summary>
        /// <param name="paths">An arbitrary length array of paths to combine together.</param>
        /// <returns>The combined path</returns>
        public static string CombineUniversal(params string[] paths)
        {
            if (!paths.Any()) return string.Empty;
            if (paths.Length == 1) return paths[0];

            var builder = new StringBuilder(paths[0]);
            for (var i = 1; i < paths.Length; i++)
            {
                builder.Append(Path.AltDirectorySeparatorChar);
                builder.Append(paths[i]);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Writes json out to a file and registers the file with the simulation manager.
        /// </summary>
        /// <param name="filePath">The path to write to.</param>
        /// <param name="json">The json information to write out.</param>
        /// <returns>Did it work correctly</returns>
        public static void WriteAndReportJsonFile(string filePath, JToken json)
        {
            var stringWriter = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture);
            using (var jsonTextWriter = new JsonTextWriter(stringWriter))
            {
                jsonTextWriter.Formatting = Formatting.Indented;
                json.WriteTo(jsonTextWriter);
            }

            var contents = stringWriter.ToString();
            File.WriteAllText(filePath, contents);
        }

        /// <summary>
        /// Writes image out to a file and registers the file with the simulation manager.
        /// </summary>
        /// <param name="path">The path to write to.</param>
        /// <param name="bytes">The image bytes.</param>
        /// <returns>Did it work properly?</returns>
        public static void WriteAndReportImageFile(string path, byte[] bytes)
        {
            var file = File.Create(path, 4096);
            file.Write(bytes, 0, bytes.Length);
            file.Close();
        }
    }
}
