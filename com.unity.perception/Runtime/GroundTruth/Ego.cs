using System;
using UnityEngine;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Marker indicating the GameObject is the root of the ego for a set of sensors. Works with <see cref="PerceptionCamera"/>.
    /// Custom sensors can use the <see cref="EgoHandle"/> property to register themselves under this ego.
    /// </summary>
    public class Ego : MonoBehaviour
    {
        /// <summary>
        /// A human-readable description for this Ego to be included in the dataset.
        /// </summary>
        public string Description;
        EgoHandle m_EgoHandle;

        /// <summary>
        /// The EgoHandle registered with DatasetCapture at runtime.
        /// </summary>
        public EgoHandle EgoHandle
        {
            get
            {
                EnsureEgoInitialized();
                return m_EgoHandle;
            }
        }

        void Start()
        {
            EnsureEgoInitialized();
        }

        void EnsureEgoInitialized()
        {
            if (m_EgoHandle == default)
                m_EgoHandle = DatasetCapture.RegisterEgo(Description);
        }
    }
}
