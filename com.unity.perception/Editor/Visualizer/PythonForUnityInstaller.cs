#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
using System;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Threading;

namespace UnityEditor.Perception.Visualizer
{
    [InitializeOnLoad]
    internal static class PythonForUnityInstaller
    {
        static PythonForUnityInstaller()
        {
            Add();
        }
        
        internal static void Add()
        {
            if (!checkIfPackageInstalled())
            {
                AddRequest request = Client.Add("com.unity.scripting.python@4.0.0-exp.5");

                while (!request.IsCompleted)
                {
                    Thread.Sleep(100);
                }
                if (request.Status == StatusCode.Success)
                    Debug.Log("Installed: " + request.Result.packageId);
                else if (request.Status >= StatusCode.Failure)
                    Debug.Log(request.Error.message);
            }
        }

        static bool checkIfPackageInstalled()
        {
            ListRequest request = Client.List();
            while (!request.IsCompleted)
            {
                Thread.Sleep(100);
            }
            if (request.Status == StatusCode.Success)
            {
                foreach (var package in request.Result)
                {
                    if (package.packageId.Contains("com.unity.scripting.python"))
                    {
                        return true;
                    }
                }
            }
            else if (request.Status >= StatusCode.Failure)
                Debug.LogError(request.Error.message);
            return false;
        }
    }
}
#endif
