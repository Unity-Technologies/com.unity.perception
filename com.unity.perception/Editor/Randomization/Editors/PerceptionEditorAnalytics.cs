using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor.Perception.Randomization
{
    static class PerceptionEditorAnalytics
    {
        const string k_VendorKey = "unity.perception";
        const string k_RunInUnitySimulationName = "runinunitysimulation";
        static int k_MaxItems = 100;
        static int k_MaxEventsPerHour = 100;

        static bool s_IsRegistered;

        static bool TryRegisterEvents()
        {
            if (s_IsRegistered)
                return true;

            var success = true;
            success &= EditorAnalytics.RegisterEventWithLimit(k_RunInUnitySimulationName, k_MaxEventsPerHour, k_MaxItems,
                k_VendorKey) == AnalyticsResult.Ok;

            s_IsRegistered = success;
            return success;
        }

        public static void ReportRunInUnitySimulationStarted(Guid runId, int totalIterations, int instanceCount, string existingBuildId)
        {
            if (!TryRegisterEvents())
                return;

            var data = new RunInUnitySimulationData
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

            var data = new RunInUnitySimulationData
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

            var data = new RunInUnitySimulationData
            {
                runId = runId.ToString(),
                runExecutionId = runExecutionId,
                runStatus = RunStatus.Succeeded.ToString()
            };
            EditorAnalytics.SendEventWithLimit(k_RunInUnitySimulationName, data);
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
    }
}
