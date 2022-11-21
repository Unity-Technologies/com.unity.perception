using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Analytics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.ReportMetadata;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Scenarios;
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
    /// <see cref="TryRegisterPerceptionAnalyticsEvent" /> with the event name defined in step 1.
    /// Note: Remember to use the conditional "#if UNITY_EDITOR" if adding editor analytics.
    /// </remarks>
    static class PerceptionAnalytics
    {
        const string k_VendorKey = "unity.perception";
        const int k_MaxElementsInStruct = 100;
        const int k_MaxEventsPerHour = 100;

        #region Setup

        [RuntimeInitializeOnLoadMethod]
        static void OnInitializeOnLoad()
        {
            DatasetCapture.SimulationEnding += OnSimulationShutdown;
        }

        static void OnSimulationShutdown()
        {
            var perceptionCameras = Object.FindObjectsOfType<PerceptionCamera>();
            ReportScenarioInformation(
                perceptionCameras,
                ScenarioBase.activeScenario
            );
        }

        #endregion

        /// <summary>
        /// Stores whether each event has been registered successfully or not.
        /// </summary>
        static Dictionary<AnalyticsEvent, bool> s_EventRegistrationStatus = new Dictionary<AnalyticsEvent, bool>();

        #region Event Definitions
        static readonly AnalyticsEvent k_EventScenarioInformation = new AnalyticsEvent(
            "perceptionScenarioInformation", AnalyticsEventType.RuntimeAndEditor, 1
        );

        /// <summary>
        /// All supported events. If an event does not exist in this list, an error will be thrown during
        /// <see cref="TryRegisterPerceptionAnalyticsEvent" />.
        /// </summary>
        static IEnumerable<AnalyticsEvent> allEvents => new[]
        {
            k_EventScenarioInformation
        };
        #endregion

        #region Helpers

        /// <summary>
        /// Tries to register an event and returns whether it was registered successfully. The result is also cached in
        /// the <see cref="s_EventRegistrationStatus" /> dictionary.
        /// </summary>
        /// <param name="theEvent">The name of the event.</param>
        /// <returns>Whether the event was successfully registered/</returns>
        static bool TryRegisterPerceptionAnalyticsEvent(AnalyticsEvent theEvent)
        {
            // Make sure the event exists in the dictionary
            if (!s_EventRegistrationStatus.ContainsKey(theEvent))
            {
                if (allEvents.Contains(theEvent))
                    s_EventRegistrationStatus[theEvent] = false;
                else
                    throw new NotSupportedException($"Unrecognized event {theEvent} not included in {nameof(allEvents)}.");
            }

            // If registered previously, return true
            if (s_EventRegistrationStatus[theEvent])
                return true;

            // Try registering the event and update the dictionary accordingly
            s_EventRegistrationStatus[theEvent] = true;
#if UNITY_EDITOR
            var status = EditorAnalytics.RegisterEventWithLimit(theEvent.name, k_MaxEventsPerHour, k_MaxElementsInStruct, k_VendorKey);
#else
            var status = UnityEngine.Analytics.Analytics.RegisterEvent(theEvent.name, k_MaxEventsPerHour, k_MaxElementsInStruct, k_VendorKey);
#endif
            s_EventRegistrationStatus[theEvent] &= status == AnalyticsResult.Ok;

            return s_EventRegistrationStatus[theEvent];
        }

        /// <summary>
        /// Based on the value of type for <see cref="theEvent" />, sends an Editor Analytics event,
        /// a Runtime Analytics event, or both.
        /// </summary>
        /// <param name="theEvent">The analytics event.</param>
        /// <param name="data">Payload of the event.</param>
        static void SendPerceptionAnalyticsEvent(AnalyticsEvent theEvent, object data)
        {
#if UNITY_EDITOR
            if (theEvent.type == AnalyticsEventType.Editor || theEvent.type == AnalyticsEventType.RuntimeAndEditor)
            {
                EditorAnalytics.SendEventWithLimit(theEvent.name, data, theEvent.versionId);
            }
#else
            if (theEvent.type == AnalyticsEventType.Runtime || theEvent.type == AnalyticsEventType.RuntimeAndEditor)
            {
                UnityEngine.Analytics.Analytics.SendEvent(theEvent.name, data, theEvent.versionId, theEvent.prefix);
            }
#endif
        }

        #endregion

        #region Event: Scenario Information

        /// <summary>
        /// Which labelers will be identified and included in the analytics information.
        /// </summary>
        public static readonly string[] labelerAllowList = new[]
        {
            nameof(BoundingBox3DLabeler), nameof(BoundingBox2DLabeler), nameof(InstanceSegmentationLabeler),
            nameof(KeypointLabeler), nameof(ObjectCountLabeler), nameof(SemanticSegmentationLabeler),
            nameof(RenderedObjectInfoLabeler), nameof(DepthLabeler), nameof(PixelPositionLabeler),
            nameof(NormalLabeler), nameof(OcclusionLabeler), nameof(MetadataReporterLabeler)
        };

        internal static Action<ScenarioCompletedData> scenarioCompletedAnalyticsSent;
        internal static void ReportScenarioInformation(
            PerceptionCamera[] cameras,
            ScenarioBase scenario,
            Action<ScenarioCompletedData> callback = null
        )
        {
            if (!TryRegisterPerceptionAnalyticsEvent(k_EventScenarioInformation))
                return;

            var data = ScenarioCompletedData.FromCamerasAndRandomizers(cameras, scenario);
            callback?.Invoke(data);
            SendPerceptionAnalyticsEvent(k_EventScenarioInformation, data);
        }

        #endregion
    }
}
