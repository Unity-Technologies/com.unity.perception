using System;

namespace UnityEngine.Perception.Randomization
{
    /// <summary>
    /// Used to annotate an AssetSourceLocation with description notes within the inspector UI
    /// </summary>
    public class AssetSourceEditorDescriptionAttribute : Attribute
    {
        /// <summary>
        /// The text notes to display in the AssetSourceLocation's UI.
        /// </summary>
        public string notes;

        /// <summary>
        /// Constructs a new AssetSourceLocationNotes attribute
        /// </summary>
        /// <param name="notes">The text notes to display in the inspector</param>
        public AssetSourceEditorDescriptionAttribute(string notes)
        {
            this.notes = notes;
        }
    }
}
