using System.Diagnostics;
using System.Threading;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

public class PyrceptionInstaller : EditorWindow
{  

    [MenuItem("Window/General/Pyrception/Run")]
    static void RunPyrception()
    {
        string path = Application.dataPath.Replace("/Assets", "");
#if UNITY_EDITOR_WIN
        path = path.Replace("/", "\\");
#endif
        string command = "";

#if UNITY_EDITOR_WIN
        command = $"cd {path}\\DataInsightsEnv\\Scripts\\ && activate && pyrception-utils.exe preview --data=\"{PlayerPrefs.GetString(SimulationState.latestOutputDirectoryKey)}/..\"";
#elif UNITY_EDITOR_OSX
        command = $"cd {path}/DataInsightsEnv/Scripts && activate && pyrception-utils preview --data=\"{PlayerPrefs.GetString(SimulationState.latestOutputDirectoryKey)}/..\"";
#endif
        int ExitCode = ExecuteCMD(command, false, true);
        if (ExitCode != 0) return;
    }

    [MenuItem("Window/General/Pyrception/Setup")]
    static void SetupPyrception()
    {
        int steps = 3;

        string path = Application.dataPath.Replace("/Assets", "");
#if UNITY_EDITOR_WIN
        path = path.Replace("/", "\\");
#endif
        string pyrceptionPath = Path.GetFullPath("Packages/com.unity.perception/Editor/Pyrception/pyrception-utils");

        EditorUtility.DisplayProgressBar("Setting up Pyrception", "Installing virtualenv...", 0 / steps);
        int ExitCode = 0;
        ExitCode = ExecuteCMD("pip install virtualenv");
        if (ExitCode != 0) return;

        EditorUtility.DisplayProgressBar("Setting up Pyrception", "Setting up virtualenv instance...", 1f / steps);

#if UNITY_EDITOR_WIN
        ExitCode = ExecuteCMD($"virtualenv \"{path}\\DataInsightsEnv\"");
#elif UNITY_EDITOR_OSX
        ExitCode = ExecuteCMD($"virtualenv \"{path}/DataInsightsEnv"");
#endif
        if (ExitCode != 0) return;

        EditorUtility.DisplayProgressBar("Setting up Pyrception", "Getting pyrception files...", 2f / steps);

#if UNITY_EDITOR_WIN
        ExitCode = ExecuteCMD($"XCOPY /E/I/Y \"{pyrceptionPath}\" \"{path}\\DataInsightsEnv\\pyrception-util\"");
#elif UNITY_EDITOR_OSX
        ExitCode = ExecuteCMD($"\cp -r \"{pyrceptionPath}\" \"{path}/DataInsightsEnv/pyrception-util\"");
#endif
        if (ExitCode != 0) return;

        EditorUtility.DisplayProgressBar("Setting up Pyrception", "Installing pyrception utils...", 2.5f / steps);

#if UNITY_EDITOR_WIN
        ExitCode = ExecuteCMD($"\"{path}\\DataInsightsEnv\\Scripts\\activate\" && cd {path} && cd .\\DataInsightsEnv\\pyrception-util\\ && pip --no-cache-dir install -e . && deactivate");
#elif UNITY_EDITOR_OSX
        ExitCode = ExecuteCMD($"\"{path}/DataInsightsEnv/Scripts/activate\" && cd {path} && cd ./DataInsightsEnv/pyrception-util && pip --no-cache-dir install -e . && deactivate");
#endif
        if (ExitCode != 0) return;

        EditorUtility.ClearProgressBar();
    }

    private static int ExecuteCMD(string command, bool waitForExit = true, bool displayWindow = false) {
        string shell = "";
        string argument = "";

#if UNITY_EDITOR_WIN
        shell = "cmd.exe";
        argument = $"/c {command}";
#elif UNITY_EDITOR_OSX
        shell = "/bin/bash";
        argument = $"-c \"{command}\"";
#endif
        ProcessStartInfo info = new ProcessStartInfo(shell, argument);

        info.CreateNoWindow = !displayWindow;
        info.UseShellExecute = !waitForExit;

        Process cmd = Process.Start(info);
        if (!waitForExit) return 0;

        cmd.WaitForExit();
        int ExitCode = 0;
        ExitCode = cmd.ExitCode;
        if (ExitCode != 0)
        {
            UnityEngine.Debug.LogError($"Error - {ExitCode} - Failed to execute: {command}");
        }

        cmd.Close();
        return ExitCode;
    }
}
