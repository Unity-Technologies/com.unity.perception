using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Perception.GroundTruth
{
    public class JointLabel : MonoBehaviour
    {
        [Serializable]
        public class TemplateData
        {
            public KeyPointTemplate template;
            public string label;
        };
        public List<TemplateData> templateInformation;
    }
}
