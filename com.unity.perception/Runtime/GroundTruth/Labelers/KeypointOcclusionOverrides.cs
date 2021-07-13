using System;
using System.Collections.Generic;

namespace UnityEngine.Perception.GroundTruth
{
    [Serializable]
    public struct KeypointOcclusionOverride
    {
        public string label;
        public float distance;
    }

    public class KeypointOcclusionOverrides : MonoBehaviour
    {
        public float overrideDistanceScale = 1.0f;
#if false
        public Dictionary<string, float> overridesMap { get; private set; }
        public List<KeypointOcclusionOverride> keypointOcclusionOverrides;

        void Start()
        {
            overridesMap = new Dictionary<string, float>(keypointOcclusionOverrides.Count);
            foreach (var kpOverride in keypointOcclusionOverrides)
            {
                overridesMap[kpOverride.label] = kpOverride.distance;
            }
        }
#endif
    }
}
