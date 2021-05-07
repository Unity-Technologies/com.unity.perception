using System;

namespace UnityEngine.Perception.Randomization
{
    /// <summary>
    /// Used to annotate an AssetSourceLocation with notes within the inspector UI
    /// </summary>
    public class AssetSourceLocationNotes : Attribute
    {
        /// <summary>
        /// The text notes to display in the AssetSourceLocation's UI.
        /// </summary>
        public string notes;

        /// <summary>
        /// Constructs a new AssetSourceLocationNotes attribute
        /// </summary>
        /// <param name="notes">The text notes to display in the inspector</param>
        public AssetSourceLocationNotes(string notes)
        {
            this.notes = notes;
        }
    }
}
