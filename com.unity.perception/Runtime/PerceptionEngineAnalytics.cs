using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Randomizers;
using EngineAnalytics = UnityEngine.Analytics.Analytics;

namespace UnityEngine.Perception
{
    static class PerceptionEngineAnalytics
    {
        const string k_VendorKey = "unity.perception";
        // Events
        /// <summary>
        /// When a Scenario completes in editor, player, or USim
        /// </summary>
        const string k_ScenarioCompletedName = "perceptionscenariocompleted";
/*
        static int k_MaxItems = 100;
        static int k_MaxEventsPerHour = 100;
*/
        static bool s_IsRegistered;

        static readonly string[] LabelAllowlist = new[]
        {
            "BoundingBox2DLabeler"
        };

        static bool TryRegisterEvents()
        {

            if (s_IsRegistered)
                return true;

            var success = true;
            /*
            success &= EngineAnalytics.RegisterEvent(k_ScenarioCompletedName, k_MaxEventsPerHour, k_MaxItems,
                k_VendorKey) == AnalyticsResult.Ok;
            */
            s_IsRegistered = success;
            return success;
        }

        #region perceptionscenariocompleted
        struct PerceptionCameraData
        {
            [UsedImplicitly]
            public string captureTriggerMode;
            [UsedImplicitly]
            public int startAtFrame;
            [UsedImplicitly]
            public int framesBetweenCaptures;
        }
        /*struct LabelerData
        {
            [UsedImplicitly]
            public string name;
            [UsedImplicitly]
            public int labelConfigCount;
        }*/
        struct MemberData
        {
            [UsedImplicitly]
            public string name;
            [UsedImplicitly]
            public object value;
        }
        struct ParameterData
        {
            [UsedImplicitly]
            public string name;
            [UsedImplicitly]
            public string distribution;
            [UsedImplicitly]
            public (string name, string distribution)[] values;
        }
        struct RandomizerData
        {
            [UsedImplicitly]
            public string name;
            [UsedImplicitly]
            public MemberData[] members;
            [UsedImplicitly]
            public ParameterData[] parameters;
        }
        struct ScenarioCompletedData
        {
            [UsedImplicitly]
            public PerceptionCameraData perceptionCamera;
            [UsedImplicitly]
            public string[] labelers;
            [UsedImplicitly]
            public RandomizerData[] randomizers;
        }

        public static void ReportScenarioCompleted(PerceptionCamera cam, IEnumerable<Randomizer> randomizers)
        {
            if (!TryRegisterEvents())
                return;

            var randomizer = randomizers.First();
            var randomizerType = randomizer.GetType();
            var randomizerName = randomizerType.Name;
            var randomizerFields = randomizerType.GetFields(BindingFlags.Public | BindingFlags.Instance);


            var data = new ScenarioCompletedData()
            {
                perceptionCamera = (cam == null)
                    ? new PerceptionCameraData()
                    : new PerceptionCameraData()
                {
                    captureTriggerMode = cam.captureTriggerMode.ToString(),
                    startAtFrame = cam.firstCaptureFrame,
                    framesBetweenCaptures = cam.framesBetweenCaptures
                },
                labelers = (cam == null)
                    ? new string[]{}
                    : cam.labelers
                        .Select(labeler => labeler.GetType().Name)
                        .Where(labeler => LabelAllowlist.Contains(labeler)).ToArray(),
                randomizers = null
            };
            // EngineAnalytics.SendEvent(k_ScenarioCompletedName, data);
        }
        #endregion
    }
}
