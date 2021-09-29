using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Analytics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Randomizers;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Perception.Analytics
{
    /// <summary>
    /// Editor and Runtime analytics for the Perception package.
    /// </summary>
    /// <remarks>
    /// To add an event:
    /// 1. Create a constant with the name of the event (eg: <see cref="k_EventScenarioInformation"/>)
    /// 2. Add the constant to <see cref="allEvents" />
    /// 3. Create a function that will report data for the event and at the start of it call
    /// <see cref="TryRegisterEvent" /> with the event name defined in step 1.
    /// Note: Remember to use the conditional "#if UNITY_EDITOR" if adding editor analytics.
    /// </remarks>
    public static class PerceptionAnalytics
    {
        enum AnalyticsEventType
        {
            Runtime,
            Editor,
            RuntimeAndEditor
        }
        struct AnalyticsEvent
        {
            public string name { get; private set; }
            public AnalyticsEventType type { get; private set; }
            public int versionId { get; private set; }

            public AnalyticsEvent(string name, AnalyticsEventType type, int versionId)
            {
                this.name = name;
                this.type = type;
                this.versionId = versionId;
            }
        }

        const string k_VendorKey = "unity.perception";
        const int k_MaxElementsInStruct = 100;
        const int k_MaxEventsPerHour = 100;

        static Dictionary<AnalyticsEvent, bool> s_EventRegistrationStatus = new Dictionary<AnalyticsEvent, bool>();

        #region Event Definition
        static readonly AnalyticsEvent k_EventScenarioInformation = new AnalyticsEvent(
            "perceptionScenarioInformation", AnalyticsEventType.RuntimeAndEditor, 1
        );
        static readonly AnalyticsEvent k_EventRunInUnitySimulation = new AnalyticsEvent(
            "runinunitysimulation", AnalyticsEventType.Editor, 1
        );
        /// <summary>
        /// All supported events. If an event does not exist in this list, an error will be thrown during
        /// <see cref="TryRegisterEvent" />.
        /// </summary>
        static IEnumerable<AnalyticsEvent> allEvents => new[]
        {
            k_EventScenarioInformation,
            k_EventRunInUnitySimulation
        };
        #endregion

        #region Common

        /// <summary>
        /// Tries to register an event and returns whether it was registered successfully. The result is also cached in
        /// the <see cref="s_EventRegistrationStatus" /> dictionary.
        /// </summary>
        /// <param name="theEvent">The name of the event.</param>
        /// <returns>Whether the event was successfully registered/</returns>
        static bool TryRegisterEvent(AnalyticsEvent theEvent)
        {
            if (!s_EventRegistrationStatus.ContainsKey(theEvent))
            {
                if (allEvents.Contains(theEvent))
                    s_EventRegistrationStatus[theEvent] = false;
                else
                    Debug.LogError($"Unrecognized event {theEvent} not included in {nameof(allEvents)}.");
            }

            if (s_EventRegistrationStatus[theEvent])
                return true;

            s_EventRegistrationStatus[theEvent] = true;
#if UNITY_EDITOR
            var status = EditorAnalytics.RegisterEventWithLimit(theEvent.name, k_MaxEventsPerHour, k_MaxElementsInStruct, k_VendorKey);
#else
            var status = Analytics.Analytics.RegisterEvent(theEvent.name, k_MaxEventsPerHour, k_MaxElementsInStruct, k_VendorKey);
#endif
            s_EventRegistrationStatus[theEvent] &= status == AnalyticsResult.Ok;

            Debug.Log($"Registering event {theEvent.name}. Operation {(s_EventRegistrationStatus[theEvent] ? "" : "un")}successful.");

            return s_EventRegistrationStatus[theEvent];
        }

        /// <summary>
        /// Based on the value of type for <see cref="theEvent" />, sends an Editor Analytics event,
        /// a Runtime Analytics event, or both.
        /// </summary>
        /// <param name="theEvent">The analytics event.</param>
        /// <param name="data">Payload of the event.</param>
        /// <param name="prefix">If Runtime event, what is the prefix ("domain.schema")?</param>
        static void SendEvent(AnalyticsEvent theEvent, object data, string prefix = "")
        {
            Debug.Log($"Reporting {theEvent.name}.");
#if UNITY_EDITOR
            if (theEvent.type == AnalyticsEventType.Editor || theEvent.type == AnalyticsEventType.RuntimeAndEditor)
            {
                EditorAnalytics.SendEventWithLimit(theEvent.name, data, theEvent.versionId);
            }
#endif
            if (theEvent.type == AnalyticsEventType.Runtime || theEvent.type == AnalyticsEventType.RuntimeAndEditor)
            {
                UnityEngine.Analytics.Analytics.SendEvent(theEvent.name, data, theEvent.versionId, prefix);
            }
        }

        #endregion

        #region Event: Scenario Information

        /// <summary>
        /// Which labelers will be identified and included in the analytics information.
        /// </summary>
        public static readonly Type[] labelerAllowList = new[]
        {
            typeof(BoundingBox3DLabeler), typeof(BoundingBox2DLabeler), typeof(InstanceSegmentationLabeler),
            typeof(KeypointLabeler), typeof(ObjectCountLabeler), typeof(SemanticSegmentationLabeler)
        };

        internal static void ReportScenarioCompleted(PerceptionCamera cam, IEnumerable<Randomizer> randomizers)
        {
            try
            {
                if (!TryRegisterEvent(k_EventScenarioInformation))
                    return;

                var data = new ScenarioCompletedData();
                if (cam != null)
                {
                    // Perception Camera Data
                    data.perceptionCamera = new PerceptionCameraData()
                    {
                        captureTriggerMode = cam.captureTriggerMode.ToString(),
                        startAtFrame = cam.firstCaptureFrame,
                        framesBetweenCaptures = cam.framesBetweenCaptures
                    };

                    // Labeler Data
                    data.labelers = cam.labelers
                        .Select(LabelerData.FromLabeler)
                        .Where(labeler => labeler != null).ToArray();
                }

                var randomizerList = randomizers.ToArray();
                if (randomizerList.Any())
                {
                    data.randomizers = randomizerList
                        .Select(RandomizerData.FromRandomizer)
                        .Where(x => x != null).ToArray();
                }

                SendEvent(k_EventScenarioInformation, data);
            }
            catch
            {
                //ignored
            }
        }

        #endregion

        #region Event: Run In Unity Simulation
        public static void ReportRunInUnitySimulationStarted(Guid runId, int totalIterations, int instanceCount, string existingBuildId)
        {
            if (!TryRegisterEvent(k_EventRunInUnitySimulation))
                return;

            var data = new RunInUnitySimulationData
            {
                runId = runId.ToString(),
                totalIterations = totalIterations,
                instanceCount = instanceCount,
                existingBuildId = existingBuildId,
                runStatus = RunStatus.Started.ToString()
            };

            SendEvent(k_EventRunInUnitySimulation, data);
        }

        public static void ReportRunInUnitySimulationFailed(Guid runId, string errorMessage)
        {
            if (!TryRegisterEvent(k_EventRunInUnitySimulation))
                return;

            var data = new RunInUnitySimulationData
            {
                runId = runId.ToString(),
                errorMessage = errorMessage,
                runStatus = RunStatus.Failed.ToString()
            };

            SendEvent(k_EventRunInUnitySimulation, data);
        }

        public static void ReportRunInUnitySimulationSucceeded(Guid runId, string runExecutionId)
        {
            if (!TryRegisterEvent(k_EventRunInUnitySimulation))
                return;

            var data = new RunInUnitySimulationData
            {
                runId = runId.ToString(),
                runExecutionId = runExecutionId,
                runStatus = RunStatus.Succeeded.ToString()
            };

            SendEvent(k_EventRunInUnitySimulation, data);
        }

        #endregion
    }
}
