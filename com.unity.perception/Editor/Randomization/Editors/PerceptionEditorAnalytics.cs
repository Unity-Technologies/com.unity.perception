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
        const string k_RunInUnitySimulationName = "runinunitysimulation";

        static bool s_IsRegistered = false;

        static bool TryRegisterEvents()
        {
            if (s_IsRegistered)
                return true;

            bool success = true;
            success &= EditorAnalytics.RegisterEventWithLimit(k_RunInUnitySimulationName, k_MaxEventsPerHour, k_MaxItems,
                k_VendorKey) == AnalyticsResult.Ok;

            s_IsRegistered = success;
            return success;
        }

        enum RunStatus
        {
            Started,
            Failed,
            Succeeded
        }

        struct RunInUnitySimulationData
        {
            [UsedImplicitly]
            public string runId;
            [UsedImplicitly]
            public int totalIterations;
            [UsedImplicitly]
            public int instanceCount;
            [UsedImplicitly]
            public string existingBuildId;
            [UsedImplicitly]
            public string errorMessage;
            [UsedImplicitly]
            public string runExecutionId;
            [UsedImplicitly]
            public string runStatus;
        }

        public static void ReportRunInUnitySimulationStarted(Guid runId, int totalIterations, int instanceCount, string existingBuildId)
        {
            if (!TryRegisterEvents())
                return;

            var data = new RunInUnitySimulationData()
            {
                runId = runId.ToString(),
                totalIterations = totalIterations,
                instanceCount = instanceCount,
                existingBuildId = existingBuildId,
                runStatus = RunStatus.Started.ToString()
            };
            EditorAnalytics.SendEventWithLimit(k_RunInUnitySimulationName, data);
        }

        public static void ReportRunInUnitySimulationFailed(Guid runId, string errorMessage)
        {
            if (!TryRegisterEvents())
                return;

            var data = new RunInUnitySimulationData()
            {
                runId = runId.ToString(),
                errorMessage = errorMessage,
                runStatus = RunStatus.Failed.ToString()
            };
            EditorAnalytics.SendEventWithLimit(k_RunInUnitySimulationName, data);
        }

        public static void ReportRunInUnitySimulationSucceeded(Guid runId, string runExecutionId)
        {
            if (!TryRegisterEvents())
                return;

            var data = new RunInUnitySimulationData()
            {
                runId = runId.ToString(),
                runExecutionId = runExecutionId,
                runStatus = RunStatus.Succeeded.ToString()
            };
            EditorAnalytics.SendEventWithLimit(k_RunInUnitySimulationName, data);
        }
    }
}
