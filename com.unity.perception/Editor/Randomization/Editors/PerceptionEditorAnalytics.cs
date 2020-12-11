using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.Analytics;

namespace UnityEngine.Perception.Randomization.Editor
{
    static class PerceptionEditorAnalytics
    {
        static int k_MaxItems = 100;
        static int k_MaxEventsPerHour = 100;
        const string k_VendorKey = "unity.perception";
        const string k_RunInUnitySimulationSucceededName = "RunInUnitySimulationSucceeded";
        const string k_RunInUnitySimulationBeginName = "RunInUnitySimulationBegin";
        const string k_RunInUnitySimulationFailedName = "RunInUnitySimulationFailed";

        static bool k_IsRegistered = false;

        static bool TryRegisterEvents()
        {
            if (k_IsRegistered)
                return true;

            bool success = true;
            success &= EditorAnalytics.RegisterEventWithLimit(k_RunInUnitySimulationBeginName, k_MaxEventsPerHour, k_MaxItems,
                k_VendorKey) == AnalyticsResult.Ok;
            success &= EditorAnalytics.RegisterEventWithLimit(k_RunInUnitySimulationFailedName, k_MaxEventsPerHour, k_MaxItems,
                k_VendorKey) == AnalyticsResult.Ok;
            success &= EditorAnalytics.RegisterEventWithLimit(k_RunInUnitySimulationSucceededName, k_MaxEventsPerHour, k_MaxItems,
                k_VendorKey) == AnalyticsResult.Ok;

            k_IsRegistered = success;
            return success;
        }

        struct RunInUnitySimulationBeginData
        {
            [UsedImplicitly]
            public Guid runGuid;
            [UsedImplicitly]
            public int totalIterations;
            [UsedImplicitly]
            public int instanceCount;
            [UsedImplicitly]
            public string existingBuildId;
        }

        public static void ReportRunInUnitySimulationBegin(Guid runGuid, int totalIterations, int instanceCount, string existingBuildId)
        {
            if (!TryRegisterEvents())
                return;

            var data = new RunInUnitySimulationBeginData()
            {
                runGuid = runGuid,
                totalIterations = totalIterations,
                instanceCount = instanceCount,
                existingBuildId = existingBuildId
            };
            EditorAnalytics.SendEventWithLimit(k_RunInUnitySimulationBeginName, data);
        }

        struct RunInUnitySimulationFailedData
        {
            [UsedImplicitly]
            public Guid runGuid;
            [UsedImplicitly]
            public string errorMessage;
        }

        public static void ReportRunInUnitySimulationFailed(Guid runGuid, string errorMessage)
        {
            if (!TryRegisterEvents())
                return;

            var data = new RunInUnitySimulationFailedData()
            {
                runGuid = runGuid,
                errorMessage = errorMessage
            };
            EditorAnalytics.SendEventWithLimit(k_RunInUnitySimulationFailedName, data);
        }

        struct RunInUnitySimulationSucceededData
        {
            [UsedImplicitly]
            public Guid runGuid;
            [UsedImplicitly]
            public string runExecutionId;
        }

        public static void ReportRunInUnitySimulationSucceeded(Guid runGuid, string runExecutionId)
        {
            if (!TryRegisterEvents())
                return;

            var data = new RunInUnitySimulationSucceededData()
            {
                runGuid = runGuid,
                runExecutionId = runExecutionId
            };
            EditorAnalytics.SendEventWithLimit(k_RunInUnitySimulationSucceededName, data);
        }
    }
}
