using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEngine.Perception.GroundTruth.Consumers
{
    /// <summary>
    /// Utility class to keep all path utils in one place
    /// </summary>
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
        /// '/' on windows, mac, and linux machines so that paths written to json are consistent.
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
        public static void WriteAndReportImageFile(string path, byte[] bytes)
        {
            var file = File.Create(path, 4096);
            file.Write(bytes, 0, bytes.Length);
            file.Close();
        }

        /// <summary>
        /// Checks to see if the passed in filename contains illegal characters. The passed
        /// in name should be the ultimate filename and not include the full path. All path
        /// separating slashes are considered illegal characters.
        /// </summary>
        /// <param name="toCheck">The string to check</param>
        /// <returns>True if the name contains illegal characters</returns>
        public static bool DoesFilenameIncludeIllegalCharacters(string toCheck)
        {
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            // Verify that the name does not match the system GetInvalidFilenameChars, does not
            // contain a space, and does not start with a number and is not empty
            return Regex.IsMatch(toCheck, invalidRegStr) ||
                toCheck.Contains(" ") || Regex.IsMatch(toCheck, @"^\d+") || string.IsNullOrEmpty(toCheck);
        }

        /// <summary>
        /// Checks to see if a filename is legal. If it is not, the illegal characters will
        /// be updated to underscores (_) and returned. The passed
        /// in name should be the ultimate filename and not include the full path. All path
        /// separating slashes are considered illegal characters.
        /// This method returns true, and the unchanged string if the name is legal, false and the updated
        /// name if the passed in name is illegal.
        /// </summary>
        /// <param name="toCheck">The name to check.</param>
        /// <returns>
        /// True and unchanged name if the filename is legal, false and the updated name if the
        /// filename is illegal.
        /// </returns>
        public static (bool, string) CheckAndFixFileName(string toCheck)
        {
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            if (Regex.IsMatch(toCheck, invalidRegStr))
            {
                Debug.LogWarning($"{toCheck} contains illegal characters for a file name. Replacing all illegal characters with \"_\"");
                return (false, Regex.Replace(toCheck, invalidRegStr, "_"));
            }

            return (true, toCheck);
        }
    }
}
