using System;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers
{
    public class AddRandomizerMenuAttribute : Attribute
    {
        public string menuPath;

        public AddRandomizerMenuAttribute(string menuPath)
        {
            this.menuPath = menuPath;
        }
    }
}
