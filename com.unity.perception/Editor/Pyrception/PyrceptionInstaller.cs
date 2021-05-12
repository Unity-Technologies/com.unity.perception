using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

public class PyrceptionInstaller : EditorWindow
{
    /// <summary>
    /// Runs pyrception instance in default browser
    /// </summary>
    [MenuItem("Window/Pyrception/Run")]
    static void RunPyrception()
    {
        string path = Application.dataPath.Replace("/Assets", "");
#if UNITY_EDITOR_WIN
        path = path.Replace("/", "\\");
#endif
        string command = "";

#if UNITY_EDITOR_WIN
        command = $"cd {path}\\DataInsightsEnv\\Scripts\\ && activate && pyrception-utils.exe preview --data=\"{PlayerPrefs.GetString(SimulationState.latestOutputDirectoryKey)}/..\"";
#elif (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
        command = $"cd {path}/DataInsightsEnv/bin; activate; ./pyrception-utils preview \"--data={PlayerPrefs.GetString(SimulationState.latestOutputDirectoryKey)}/..\"";
#endif
        int ExitCode = 0;
        ExecuteCMD(command, ref ExitCode, waitForExit: false, displayWindow: true);
        if (ExitCode != 0)
            return;
    }

    /// <summary>
    /// Install pyrception (Assumes python and pip are already installed)
    /// - installs virtualenv if it is not already installed
    /// - and setups a virtual environment for pyrception
    /// </summary>
    [MenuItem("Window/Pyrception/Setup")]
    static void SetupPyrception()
    {
        int steps = 3;

        //Check pip install
        int ExitCode = 0;
        ExecuteCMD("pip3", ref ExitCode);
        if(ExitCode != 0)
        {
            UnityEngine.Debug.LogError("pip3 must be installed.");
            return;
        }

        string path = Application.dataPath.Replace("/Assets", "");
#if UNITY_EDITOR_WIN
        path = path.Replace("/", "\\");
#endif
        string pyrceptionPath = Path.GetFullPath("Packages/com.unity.perception/Editor/Pyrception/pyrception-utils").Replace("\\","/");

        EditorUtility.DisplayProgressBar("Setting up Pyrception", "Installing virtualenv...", 0 / steps);
        ExitCode = 0;
#if UNITY_EDITOR_WIN
        ExecuteCMD($"pip3 install virtualenv", ref ExitCode);
#elif (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
        ExecuteCMD($"pip3 install --target=\"{path}/virtualenvDI\" virtualenv", ref ExitCode); //(maybe add --no-user)
#endif
        if (ExitCode != 0) {
            EditorUtility.ClearProgressBar();
            return;
        }
        EditorUtility.DisplayProgressBar("Setting up Pyrception", "Setting up virtualenv instance...", 1f / steps);

        //get virtualenv actual location
        /*
        //get virtualenv actual location
        string virtualenvPath = ExecuteCMD("pip3 show virtualenv | " +
#if UNITY_EDITOR_WIN
            "findstr" +
#elif (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
            "grep" +
#endif
            " Location:", ref ExitCode, redirectOutput: true);
        if (ExitCode != 0) {
            EditorUtility.ClearProgressBar();
            return;
        }

        virtualenvPath = virtualenvPath.Replace("Location: ", "").Trim();


#if UNITY_EDITOR_WIN
        virtualenvPath += "\\..\\Scripts";
        ExecuteCMD($"{virtualenvPath}\\virtualenv.exe -p python3 \"{path}\\DataInsightsEnv\"", ref ExitCode);
#elif (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
        virtualenvPath += "/../bin";
        ExecuteCMD($"{virtualenvPath}/virtualenv -p python3 \"{path}/DataInsightsEnv\"", ref ExitCode);
#endif
        if (ExitCode != 0) {
            EditorUtility.ClearProgressBar();
            return;
        }
         */
#if (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
        string virtualenvPath = path+"/virtualenvDI/bin/";
#endif


#if UNITY_EDITOR_WIN
        ExecuteCMD($"virtualenv -p python3 \"{path}\\DataInsightsEnv\"", ref ExitCode);
#elif (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
        ExecuteCMD("export PYTHONPATH=\"${PYTHONPATH}:"+$"{path}/virtualenvDI\";"+$"\"{virtualenvPath}/virtualenv\" -p python3 \"{path}/DataInsightsEnv\"", ref ExitCode);
#endif
        if (ExitCode != 0) {
            EditorUtility.ClearProgressBar();
            return;
        }

        EditorUtility.DisplayProgressBar("Setting up Pyrception", "Getting pyrception files...", 2f / steps);

#if UNITY_EDITOR_WIN
        ExecuteCMD($"XCOPY /E/I/Y \"{pyrceptionPath}\" \"{path}\\DataInsightsEnv\\pyrception-util\"", ref ExitCode);
#elif (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
        ExecuteCMD($"\\cp -r \"{pyrceptionPath}\" \"{path}/DataInsightsEnv/pyrception-util\"", ref ExitCode);
#endif
        if (ExitCode != 0) {
            EditorUtility.ClearProgressBar();
            return;
        }

        EditorUtility.DisplayProgressBar("Setting up Pyrception", "Installing pyrception utils...", 2.5f / steps);

#if UNITY_EDITOR_WIN
        ExecuteCMD($"\"{path}\\DataInsightsEnv\\Scripts\\activate\" && cd \"{path}\\DataInsightsEnv\\pyrception-util\" && pip3 --no-cache-dir install -e . && deactivate", ref ExitCode);
#elif (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
        ExecuteCMD($"source \"{path}/DataInsightsEnv/bin/activate\"; cd \"{path}/DataInsightsEnv/pyrception-util\"; pip3 --no-cache-dir install -e .; deactivate", ref ExitCode);
#endif
        if (ExitCode != 0) {
            EditorUtility.ClearProgressBar();
            return;
        }

        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// Executes command in cmd or console depending on system
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="waitForExit">Should it wait for exit before returning to the editor (i.e. is it not async?)</param>
    /// <param name="displayWindow">Should the command window be displayed</param>
    /// <returns></returns>
    private static string ExecuteCMD(string command, ref int ExitCode, bool waitForExit = true, bool displayWindow = false, bool redirectOutput = false)
    {
        string shell = "";
        string argument = "";
        string output = "";

#if UNITY_EDITOR_WIN
        shell = "cmd.exe";
        argument = $"/c \"{command}\"";
#elif (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
        shell = "/bin/bash";
        argument = $"-c \"{command}\"";
#endif
        ProcessStartInfo info = new ProcessStartInfo(shell, argument);

        info.CreateNoWindow = !displayWindow;
        info.UseShellExecute = !waitForExit;
        info.RedirectStandardOutput = redirectOutput && waitForExit;
        info.RedirectStandardError = true && waitForExit;

        Process cmd = Process.Start(info);
        if (!waitForExit)
            return "";

        cmd.WaitForExit();
        if (redirectOutput) {
            output = cmd.StandardOutput.ReadToEnd();
        }

        ExitCode = cmd.ExitCode;
        if (ExitCode != 0)
        {
            UnityEngine.Debug.LogError($"Error - {ExitCode} - Failed to execute: {command} - {cmd.StandardError.ReadToEnd()}");
        }

        cmd.Close();

        return output;
    }
}
