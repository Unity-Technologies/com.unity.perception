using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.Labelers;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Label to designate a custom joint/keypoint. These are needed to add body
    /// parts to a humanoid model that are not contained in its <see cref="Animator"/> <see cref="Avatar"/>
    ///
    /// These label's can also be applied to the keypoints found in the <see cref="Avatar"/> to override the self
    /// occlusion tolerance values defined in the <see cref="KeypointTemplate"/> file. The <see cref="KeypointTemplate"/>
    /// defines the typical tolerances for a model, but certain models may need to have specific overrides on a keypoint
    /// for the self occlusion to work properly.
    /// </summary>
    [AddComponentMenu("Perception/Labeling/Joint Label")]
    [Serializable]
    public class JointLabel : MonoBehaviour, ISerializationCallbackReceiver
    {
        static PerceptionCamera s_SinglePerceptionCamera;

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
        List<TemplateData> templateInformation;

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
            if (s_SinglePerceptionCamera == null)
            {
                s_SinglePerceptionCamera = FindObjectOfType<PerceptionCamera>();
            }

            Mesh sphereMesh = null;
#if UNITY_EDITOR
            var defaultAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Library/unity default resources");
            sphereMesh = (Mesh)defaultAssets.FirstOrDefault(a => a.name == "Sphere");

#endif
            float occlusionDistance;
            if (this.overrideSelfOcclusionDistance)
            {
                occlusionDistance = selfOcclusionDistance;
            }
            else
            {
                if (s_SinglePerceptionCamera == null)
                {
                    occlusionDistance = KeypointDefinition.defaultSelfOcclusionDistance;
                }
                else
                {
                    var keypointLabeler = (KeypointLabeler)s_SinglePerceptionCamera.labelers.FirstOrDefault(l => l is KeypointLabeler);
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
