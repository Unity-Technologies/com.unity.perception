using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Label to designate a custom joint/keypoint. These are needed to add body
    /// parts to a humanoid model that are not contained in its <see cref="Animator"/> <see cref="Avatar"/>
    /// </summary>
    [AddComponentMenu("Perception/Labeling/Joint Label")]
    public class JointLabel : MonoBehaviour
    {
        /// <summary>
        /// Maps this joint to a joint in a <see cref="KeypointTemplate"/>
        /// </summary>
        [Serializable]
        public class TemplateData
        {
            /// <summary>
            /// The <see cref="KeypointTemplate"/> that defines this joint.
            /// </summary>
            public KeypointTemplate template;
            /// <summary>
            /// The name of the joint.
            /// </summary>
            public string label;
        };

        /// <summary>
        /// List of all of the templates that this joint can be mapped to.
        /// </summary>
        public List<TemplateData> templateInformation;
    }
}
