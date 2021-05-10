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

        int ExitCode = 0;
        ExitCode = ExecuteCMD("pip install virtualenv");
        if (ExitCode != 0) return;

        ExitCode = ExecuteCMD($"virtualenv \"{path}\\DataInsightsEnv\"");
        if (ExitCode != 0) return;

        ExitCode = ExecuteCMD($"XCOPY /E/I/Y \"{pyrceptionPath}\" \"{path}\\DataInsightsEnv\\pyrception-util\"");
        if (ExitCode != 0) return;

        ExitCode = ExecuteCMD($"cd \"{path}\\DataInsightsEnv\\pyrception-util\"");
        if (ExitCode != 0) return;

        ExitCode = ExecuteCMD($"\"{path}\\DataInsightsEnv\\Scripts\\activate\" && cd {path} && cd .\\DataInsightsEnv\\pyrception-util\\ && pip --no-cache-dir install -e . && deactivate");
        if (ExitCode != 0) return;
    }

    private static int ExecuteCMD(string command) {
        ProcessStartInfo info = new ProcessStartInfo("cmd.exe", "/c " + command);
        info.CreateNoWindow = true;
        info.UseShellExecute = false;

        Process cmd = Process.Start(info);
        cmd.WaitForExit();

        int ExitCode = 0;
        ExitCode = cmd.ExitCode;
        if (ExitCode != 0)
        {
            UnityEngine.Debug.LogError($"Error - {ExitCode} - Failed to execute: {command}");
        }

        return cmd.ExitCode;
    }

    private static void RevertChanges() {
        return;
    }
}
