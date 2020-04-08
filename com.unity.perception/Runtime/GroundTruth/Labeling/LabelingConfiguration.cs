using System;
using System.Collections.Generic;

namespace UnityEngine.Perception.Sensors
{
    [CreateAssetMenu(fileName = "LabelingConfiguration", menuName = "Perception/Labeling Configuration", order = 1)]
    public class LabelingConfiguration : ScriptableObject
    {
        [SerializeField]
        public List<LabelingConfigurationEntry> LabelingConfigurations = new List<LabelingConfigurationEntry>();

        public bool TryGetMatchingConfigurationIndex(Labeling labeling, out int index)
        {
            foreach (var labelingClass in labeling.classes)
            {
                for (var i = 0; i < LabelingConfigurations.Count; i++)
                {
                    var configuration = LabelingConfigurations[i];
                    if (string.Equals(configuration.label, labelingClass))
                    {
                        index = i;
                        return true;
                    }
                }
            }

            index = -1;
            return false;
        }
    }

    [Serializable]
    public struct LabelingConfigurationEntry
    {
        public string label;
        public int value;
        public LabelingConfigurationEntry(string label, int value)
        {
            this.label = label;
            this.value = value;
        }
    }
}
