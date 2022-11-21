using System;
using UnityEngine.Perception.GroundTruth;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// A scenario that runs for a fixed number of frames during each iteration
    /// </summary>
    [AddComponentMenu("Perception/Scenarios/Fixed Length Scenario")]
    public class FixedLengthScenario : PerceptionScenario<FixedLengthScenario.Constants>
    {
        /// <summary>
        /// Constants describing the execution of this scenario
        /// </summary>
        [Serializable]
        public class Constants : ScenarioConstants
        {
            /// <summary>
            /// The index of the first iteration to execute. The random seed for the randomizers in an iteration are
            /// determined by the global <see cref="ScenarioConstants.randomSeed"/> and the iteration index.
            /// </summary>
            [Tooltip("The index of the first iteration to execute.")]
            public int startIteration;

            /// <summary>
            /// The number of iterations to run, starting at startIteration.
            /// </summary>
            [Tooltip("The number of iterations to run.")]
            public int iterationCount = 100;

            [HideInInspector]
            [SerializeField]
            internal int totalIterations = 100;

            /// <summary>
            /// The number of Unity Simulation instances assigned to execute this scenario. The total number of iterations (N) will be divided by the number of instances (M), so each instance will run for N/M iterations.
            /// </summary>
            [HideInInspector]
            [SerializeField]
            internal int instanceCount = 1;

            /// <summary>
            /// The Unity Simulation instance index of the currently executing worker.
            /// </summary>
            [HideInInspector]
            [SerializeField]
            internal int instanceIndex;
        }

        /// <summary>
        /// The number of frames to render per iteration.
        /// </summary>
        [Tooltip("The number of frames to render per iteration.")]
        public int framesPerIteration = 1;

        /// <summary>
        /// This serialized progress field is used by the Scenario inspector to track the scenario's completion progress.
        /// </summary>
        [SerializeField, HideInInspector]
        float m_ProgressPercentage;

        int m_RestoredFrameId;

        internal bool m_SimulationRunningInCloudOverride = false;

        /// <summary>
        /// The proportion of the scenario iterations that have been completed expressed as a percentage.
        /// </summary>
        internal float progressPercentage => m_ProgressPercentage;

        /// <summary>
        /// Tries to restore previous simulation state
        /// </summary>
        protected override void OnResumeSimulation()
        {
            var(status, lastFrame) = DatasetCapture.activateEndpoint.ResumeSimulationFromCrash(constants.iterationCount);
            SimulationState.frameOffset = lastFrame;
            SimulationState.sequenceId = lastFrame - 1;
            SimulationState.dataCaptured = true;
            m_RestoredFrameId = lastFrame;
        }

        /// <summary>
        /// OnAwake is called when this scenario MonoBehaviour is created or instantiated
        /// </summary>
        protected override void OnAwake()
        {
            base.OnAwake();

            if (!IsSimulationRunningInCloud())
            {
                currentIteration = constants.startIteration;
            }

            if (m_RestoredFrameId != 0)
            {
                currentIteration = m_RestoredFrameId + 1;
                constants.startIteration = m_RestoredFrameId + 1;
                constants.iterationCount -= m_RestoredFrameId;
            }
        }

        /// <summary>
        /// Loads Configuration file
        /// </summary>
        protected override void LoadConfigurationAsset()
        {
            base.LoadConfigurationAsset();
            if (IsSimulationRunningInCloud())
                constants.startIteration = 0;
        }

        bool IsSimulationRunningInCloud()
        {
#if UNITY_SIMULATION_CORE_PRESENT
            if (Unity.Simulation.Configuration.Instance.IsSimulationRunningInCloud())
                return true;
#endif
            return m_SimulationRunningInCloudOverride;
        }

        /// <inheritdoc/>
        protected sealed override bool isScenarioComplete
        {
            get
            {
#if UNITY_SIMULATION_CORE_PRESENT
                return IsSimulationRunningInCloud() ? currentIteration >= constants.totalIterations : currentIteration >= constants.iterationCount + constants.startIteration;
#else
                return currentIteration >= constants.iterationCount;
#endif
            }
        }

        /// <inheritdoc/>
        protected override bool isIterationComplete => currentIterationFrame >= framesPerIteration;

#if UNITY_SIMULATION_CORE_PRESENT
        protected sealed override void IncrementIteration()
        {
            if (IsSimulationRunningInCloud())
            {
                currentIteration += constants.instanceCount;
            }
            else
            {
                base.IncrementIteration();
            }
        }

#endif
        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            base.OnUpdate();

#if UNITY_SIMULATION_CORE_PRESENT
            if (IsSimulationRunningInCloud())
            {
                return;
            }
#endif
            if (constants == null)
            {
                return;
            }

            if (isScenarioComplete)
            {
                m_ProgressPercentage = 100;
            }
            else
            {
                var totalFrames = constants.iterationCount * framesPerIteration;
                var currentFrame = (currentIteration - constants.startIteration) * framesPerIteration + currentIterationFrame;
                var delta = (currentFrame + 1) / (float)totalFrames;
                m_ProgressPercentage = Mathf.Clamp01(delta) * 100f;
            }
        }
    }
}
