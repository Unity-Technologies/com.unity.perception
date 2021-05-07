using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

public class PyrceptionInstaller
{
    [MenuItem("Window/General/Pyrception/Run")]
    static void RunPyrception()
    {
        string path = Application.dataPath.Replace("/Assets","").Replace("/","\\");
        string command = $"cd {path}\\DataInsightsEnv\\Scripts\\ && activate && pyrception-utils.exe preview --data=\"{PlayerPrefs.GetString(SimulationState.latestOutputDirectoryKey)}/..\"";

        ProcessStartInfo info = new ProcessStartInfo("cmd.exe", "/c " + command);
        info.CreateNoWindow = false;
        info.UseShellExecute = true;
        Process cmd = Process.Start(info);
    }

    [MenuItem("Window/General/Pyrception/Setup")]
    static void SetupPyrception()
    {
        string path = Application.dataPath.Replace("/Assets", "").Replace("/", "\\");
        string pyrceptionPath = Path.GetFullPath("Packages/com.unity.perception/Editor/Pyrception/pyrception-utils");
        UnityEngine.Debug.Log(pyrceptionPath);
        string command =
            $"pip install virtualenv && " +
            $"cd {path} && " +
            $"virtualenv DataInsightsEnv && " +
            $"XCOPY /E/I/Y {pyrceptionPath} .\\DataInsightsEnv\\pyrception-util && " +
            $".\\DataInsightsEnv\\Scripts\\activate && " +
            $"cd .\\DataInsightsEnv\\pyrception-util && " +
            $"pip --no-cache-dir install -e .";

        ProcessStartInfo info = new ProcessStartInfo("cmd.exe", "/c " + command);
        info.CreateNoWindow = false;
        info.UseShellExecute = true;
        Process cmd = Process.Start(info);
        cmd.WaitForExit();
        int ExitCode = cmd.ExitCode;
        if(ExitCode == 0)
        {
            UnityEngine.Debug.Log("Completed setup for Dataset Insights");
        }
        else
        {
            UnityEngine.Debug.LogError("Dataset Insights setup failed");
        }
        
    }
}
