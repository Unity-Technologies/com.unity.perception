using System;

namespace UnityEngine.Perception.Randomization.Curriculum
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CurriculumMetaData : Attribute
    {
        public string typeDisplayName;

        public CurriculumMetaData(string typeDisplayName)
        {
            this.typeDisplayName = typeDisplayName;
        }
    }
}
