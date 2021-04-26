using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Label to designate a custom joint/keypoint. These are needed to add body
    /// parts to a humanoid model that are not contained in its <see cref="Animator"/> <see cref="Avatar"/>
    /// </summary>
    [AddComponentMenu("Perception/Labeling/Joint Label")]
    [Serializable]
    public class JointLabel : MonoBehaviour, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Maps this joint to a joint in a <see cref="KeypointTemplate"/>
        /// </summary>
        [Serializable]
        class TemplateData
        {
            /// <summary>
            /// The name of the joint.
            /// </summary>
            public string label;
        };

        /// <summary>
        /// List of all of the templates that this joint can be mapped to.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private List<TemplateData> templateInformation;

        /// <summary>
        /// List of all of the templates that this joint can be mapped to.
        /// </summary>
        [SerializeField]
        public List<string> labels = new List<string>();

        public bool useLocalSelfOcclusionDistance = false;
        public float selfOcclusionDistance = .15f;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (templateInformation != null)
            {
                foreach (var data in templateInformation)
                {
                    labels.Add(data.label);
                }

                templateInformation = null;
            }
        }
    }
}
