using System;
using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// A scenario that runs for a fixed number of frames during each iteration
    /// </summary>
    [AddComponentMenu("Perception/Scenarios/Fixed Length Scenario")]
    public class FixedLengthScenario: UnitySimulationScenario<FixedLengthScenario.Constants>
    {
        protected PerceptionCamera m_PerceptionCamera;

        /// <summary>
        /// Constants describing the execution of this scenario
        /// </summary>
        [Serializable]
        public class Constants : UnitySimulationScenarioConstants
        {
            /// <summary>
            /// The number of frames to render per iteration.
            /// </summary>
            [Tooltip("The number of frames to render per iteration.")]
            public int framesPerIteration = 1;
        }

        /// <summary>
        /// Returns whether the current scenario iteration has completed
        /// </summary>
        protected override bool isIterationComplete => currentIterationFrame >= constants.framesPerIteration;

        /// <inheritdoc/>
        protected override void OnAwake()
        {
            base.OnAwake();
            m_PerceptionCamera = FindObjectOfType<PerceptionCamera>();
            if (m_PerceptionCamera != null && m_PerceptionCamera.captureTriggerMode != CaptureTriggerMode.Manual)
            {
                Debug.LogError("The perception camera must be set to manual capture mode", m_PerceptionCamera);
                m_PerceptionCamera.enabled = false;
                enabled = false;
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (m_PerceptionCamera && currentIterationFrame == constants.framesPerIteration - 1)
            {
                m_PerceptionCamera.RequestCapture();
            }
        }
    }
}
