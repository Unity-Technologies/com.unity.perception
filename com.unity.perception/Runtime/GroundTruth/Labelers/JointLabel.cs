using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.Perception.GroundTruth
{
    public enum SelfOcclusionDistanceSource
    {
        JointLabel,
        KeypointLabeler
    }
    /// <summary>
    /// Label to designate a custom joint/keypoint. These are needed to add body
    /// parts to a humanoid model that are not contained in its <see cref="Animator"/> <see cref="Avatar"/>
    /// </summary>
    [AddComponentMenu("Perception/Labeling/Joint Label")]
    [Serializable]
    public class JointLabel : MonoBehaviour, ISerializationCallbackReceiver
    {
        private static PerceptionCamera singlePerceptionCamera = null;

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

        /// <summary>
        /// Whether <see cref="selfOcclusionDistance"/> should be used instead of the one specified in the <see cref="KeypointTemplate"/>.
        /// </summary>
        public bool overrideSelfOcclusionDistance = false;
        /// <summary>
        /// Whether <see cref="selfOcclusionDistance"/> should be used instead of the one specified in the <see cref="KeypointTemplate"/>.
        /// </summary>
        public float selfOcclusionDistance = .15f;

        /// <summary>
        /// Internal method for serialization.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        /// <summary>
        /// Internal method for serialization.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
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

        private void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, "Packages/com.unity.perception/Editor/Icons/Keypoint.png", false);
        }

        private void OnDrawGizmosSelected()
        {
            if (singlePerceptionCamera == null)
            {
                singlePerceptionCamera = FindObjectOfType<PerceptionCamera>();
            }

            Mesh sphereMesh = null;
#if UNITY_EDITOR
            var defaultAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Library/unity default resources");
            sphereMesh = (Mesh) defaultAssets.FirstOrDefault(a => a.name == "Sphere");

#endif
            float occlusionDistance;
            if (this.overrideSelfOcclusionDistance)
            {
                occlusionDistance = selfOcclusionDistance;
            }
            else
            {
                if (singlePerceptionCamera == null)
                {
                    occlusionDistance = KeypointDefinition.defaultSelfOcclusionDistance;
                }
                else
                {
                    var keypointLabeler = (KeypointLabeler) singlePerceptionCamera.labelers.FirstOrDefault(l => l is KeypointLabeler);
                    var template = keypointLabeler?.activeTemplate;
                    if (template == null)
                        occlusionDistance = KeypointDefinition.defaultSelfOcclusionDistance;
                    else
                    {
                        KeypointDefinition matchingKeypoint = null;
                        foreach (var k in template.keypoints)
                        {
                            if (this.labels.Contains(k.label))
                            {
                                matchingKeypoint = k;
                                break;
                            }
                        }

                        if (matchingKeypoint == null)
                            occlusionDistance = KeypointDefinition.defaultSelfOcclusionDistance;
                        else
                            occlusionDistance = matchingKeypoint.selfOcclusionDistance;
                    }
                }
            }

            var occlusionDistanceScale = transform.lossyScale * occlusionDistance;
            Gizmos.color = new Color(1, 1, 1, .25f);
            Gizmos.DrawMesh(sphereMesh, 0, transform.position, transform.rotation, occlusionDistanceScale * 2);
        }
    }
}
