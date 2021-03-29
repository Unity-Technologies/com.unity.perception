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
        PerceptionCamera m_PerceptionCamera;

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
            if (m_PerceptionCamera && currentIterationFrame == constants.framesPerIteration - 1
            && currentIteration > 1)
            {
                //skip first iteration for capturing because labelers are not yet initialized. They are currently initialized at the end of the first iteration.
                //TO DO: Make scheduling more robust in order to capture first iteration too
                m_PerceptionCamera.RequestCapture();
            }
        }

        protected override void OnIterationEnd()
        {
            if (currentIteration == constants.instanceCount + 1)
            {
                //it is the penultimate frame of the first iteration, so all placement randomizers have woken up and labeled their prefabs by now
                SetupLabelConfigs();
            }
        }

        static void SetupLabelConfigs()
        {
            var perceptionCamera = FindObjectOfType<PerceptionCamera>();

            var idLabelConfig = ScriptableObject.CreateInstance<IdLabelConfig>();

            idLabelConfig.autoAssignIds = true;
            idLabelConfig.startingLabelId = StartingLabelId.One;

            var stringList = LabelManager.singleton.LabelStringsForAutoLabelConfig;

            var idLabelEntries = new List<IdLabelEntry>();
            for (var i = 0; i < stringList.Count; i++)
            {
                idLabelEntries.Add(new IdLabelEntry
                {
                    id = i,
                    label = stringList[i]
                });
            }
            idLabelConfig.Init(idLabelEntries);

            var semanticLabelConfig = ScriptableObject.CreateInstance<SemanticSegmentationLabelConfig>();
            var semanticLabelEntries = new List<SemanticSegmentationLabelEntry>();
            for (var i = 0; i < stringList.Count; i++)
            {
                semanticLabelEntries.Add(new SemanticSegmentationLabelEntry()
                {
                    label = stringList[i],
                    color = GetUniqueSemanticSegmentationColor()
                });
            }
            semanticLabelConfig.Init(semanticLabelEntries);

            foreach (var labeler in perceptionCamera.labelers)
            {
                if (!labeler.enabled)
                    continue;

                switch (labeler)
                {
                    case BoundingBox2DLabeler boundingBox2DLabeler:
                        boundingBox2DLabeler.idLabelConfig = idLabelConfig;
                        break;
                    case BoundingBox3DLabeler boundingBox3DLabeler:
                        boundingBox3DLabeler.idLabelConfig = idLabelConfig;
                        break;
                    case ObjectCountLabeler objectCountLabeler:
                        objectCountLabeler.labelConfig.autoAssignIds = idLabelConfig.autoAssignIds;
                        objectCountLabeler.labelConfig.startingLabelId = idLabelConfig.startingLabelId;
                        objectCountLabeler.labelConfig.Init(idLabelEntries);
                        break;
                    case RenderedObjectInfoLabeler renderedObjectInfoLabeler:
                        renderedObjectInfoLabeler.idLabelConfig = idLabelConfig;
                        break;
                    case KeypointLabeler keypointLabeler:
                        keypointLabeler.idLabelConfig = idLabelConfig;
                        break;
                    case InstanceSegmentationLabeler instanceSegmentationLabeler:
                        instanceSegmentationLabeler.idLabelConfig = idLabelConfig;
                        break;
                    case SemanticSegmentationLabeler semanticSegmentationLabeler:
                        semanticSegmentationLabeler.labelConfig = semanticLabelConfig;
                        break;
                }

                labeler.Init(perceptionCamera);
            }
        }

        static HashSet<Color> s_ColorsAlreadyUsed = new HashSet<Color>();
        static ColorRgbParameter s_SemanticColorParameter = new ColorRgbParameter();
        static Color GetUniqueSemanticSegmentationColor()
        {
            var seed = SamplerState.NextRandomState();
            var sampledColor = s_SemanticColorParameter.Sample();
            var maxTries = 1000;
            var count = 0;

            while (s_ColorsAlreadyUsed.Contains(sampledColor) && count <= maxTries)
            {
                count++;
                sampledColor = s_SemanticColorParameter.Sample();
                Debug.LogError("Failed to find unique semantic segmentation color for a label.");
            }

            s_ColorsAlreadyUsed.Add(sampledColor);
            return sampledColor;
        }
    }
}
