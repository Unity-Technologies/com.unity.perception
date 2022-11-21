using System;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// The AddRandomizerMenu attribute allows you to organize randomizers under different menu paths
    /// </summary>
    public class AddRandomizerMenuAttribute : Attribute
    {
        /// <summary>
        /// The assigned randomizer menu path. Path directories should be separated using forward slash characters.
        /// </summary>
        public string menuPath;

        /// <summary>
        /// Add a randomizer to the AddRandomizerMenus
        /// </summary>
        /// <param name="menuPath">The assigned randomizer menu path</param>
        public AddRandomizerMenuAttribute(string menuPath)
        {
            this.menuPath = menuPath;
        }
    }
}
