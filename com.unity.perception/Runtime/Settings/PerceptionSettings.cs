#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEditor.Perception.GroundTruth;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;

namespace UnityEngine.Perception.Settings
{
    [Serializable]
    public class PerceptionSettings : MonoBehaviour
    {
        static string gameObjectName = "_PerceptionSettings";

        static PerceptionSettings s_Instance;
        public static PerceptionSettings instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var obj = GameObject.Find(gameObjectName);
                    if (obj == null)
                    {
                        obj = new GameObject(gameObjectName);
                    }

                    s_Instance = obj.GetComponent<PerceptionSettings>();
                    if (s_Instance == null)
                    {
                        s_Instance = obj.AddComponent<PerceptionSettings>();
                    }

                    obj.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                }

                return s_Instance;
            }
        }

#if UNITY_EDITOR
        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(instance);
        }
#endif

        [SerializeReference][ConsumerEndpointDrawer(typeof(IConsumerEndpoint))]
        public IConsumerEndpoint endpoint = new PerceptionEndpoint();
    }
}
