#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Perception.Visualizer
{
    public class VisualizerInstaller : EditorWindow
    {
        //This files stores entries as ProjectDataPath,PythonPID,Port,VisualizerPID
        //It keeps a record of the instances of visualizer opened so that we don't open a new one everytime
        const string k_FilenameStreamlitInstances = "Unity/streamlit_instances.csv";

        static string PathToStreamlitInstances
        {
            get
            {
#if UNITY_EDITOR_WIN
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), k_FilenameStreamlitInstances);
#elif UNITY_EDITOR_OSX
                return Path.Combine(
                            Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library"
                            ),
                            k_FilenameStreamlitInstances);
#endif
            }
        }

        const string k_NameOfVisualizerProcess
#if UNITY_EDITOR_OSX
            = "bash";
#elif UNITY_EDITOR_WIN
            = "datasetvisualizer";
#endif

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Install visualizer (Assumes python3 and pip3 are already installed)
        /// - installs virtualenv if it is not already installed
        /// - and setups a virtual environment for visualizer
        /// </summary>
        static void SetupVisualizer()
        {
            var project = Application.dataPath;

            var (pythonPid, port, visualizerPid) = ReadEntry(project);

            //If there is a python instance for this project AND it is alive then setup will fail (must kill instance)
            if (pythonPid != -1 && ProcessAlive(pythonPid, port, visualizerPid))
            {
                if (EditorUtility.DisplayDialog("Kill visualizer?",
                    "The visualizer tool can't be running while you setup, would you like to kill the current instance?",
                    "Kill visualizer",
                    "Cancel"))
                {
                    Process.GetProcessById(pythonPid + 1).Kill();
                }
                else
                {
                    return;
                }
            }

            const int steps = 3;
            var exitCode = 0;
            string output = null;

            //==============================SETUP PATHS======================================
#if UNITY_EDITOR_WIN
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets", "/Library/PythonInstall/Scripts"));
#elif UNITY_EDITOR_OSX
            string packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets","/Library/PythonInstall/bin"));
#endif

#if UNITY_EDITOR_WIN
            packagesPath = packagesPath.Replace("/", "\\");
#endif

            //==============================INSTALL VISUALIZER IN PYTHON FOR UNITY======================================

            EditorUtility.DisplayProgressBar("Setting up the Visualizer", "Installing Visualizer (This may take a few minutes)", 2f / steps);
#if UNITY_EDITOR_WIN
            ExecuteCmd($"\"{packagesPath}\"\\pip3.bat install --upgrade --no-warn-script-location unity-cv-datasetvisualizer", ref exitCode, ref output, waitForExit: -1);
#elif UNITY_EDITOR_OSX
            ExecuteCmd($"cd \'{packagesPath}\'; ./python3.7 -m pip install --upgrade unity-cv-datasetvisualizer", ref exitCode, ref output, waitForExit: -1);
#endif
            if (exitCode != 0)
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            EditorUtility.ClearProgressBar();
            Debug.Log("Successfully installed visualizer");
        }

        /// <summary>
        /// Executes command in cmd or console depending on system
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="exitCode">int reference to get exit code</param>
        /// <param name="output">string reference to save output</param>
        /// <param name="waitForExit">Should it wait for exit before returning to the editor (i.e. is it not async?)</param>
        /// <param name="displayWindow">Should the command window be displayed</param>
        /// <param name="getOutput">Whether or not to get output</param>
        /// <returns>PID of process that started</returns>
        static int ExecuteCmd(string command, ref int exitCode, ref string output, int waitForExit = 0, bool displayWindow = false, bool getOutput = false)
        {
#if UNITY_EDITOR_WIN
            const string shell = "cmd.exe";
            var argument = $"/c \"{command}\"";
#elif UNITY_EDITOR_OSX
            const string shell = "/bin/bash";
            var argument = $"-c \"{command}\"";
#endif

            var info = new ProcessStartInfo(shell, argument);

            info.CreateNoWindow = !displayWindow;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = getOutput;
            info.RedirectStandardError = waitForExit > 0;

            var cmd = Process.Start(info);
            if (cmd == null)
            {
                Debug.LogError($"Could not create process using command {command}");
                return 0;
            }

            switch (waitForExit)
            {
                case 0:
                    return cmd.Id;
                case -1:
                    cmd.WaitForExit();
                    break;
                default:
                {
                    if (waitForExit > 0)
                    {
                        cmd.WaitForExit(waitForExit);
                    }

                    break;
                }
            }

            if (getOutput && waitForExit != 0)
            {
                output = cmd.StandardOutput.ReadToEnd();
            }

            if (cmd.HasExited)
            {
                exitCode = cmd.ExitCode;
                if (exitCode != 0)
                {
                    Debug.LogError($"Error - {exitCode} - Failed to execute: {command} - {cmd.StandardError.ReadToEnd()}");
                }
            }

            cmd?.Close();

            return 0;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// If an instance is already running for this project it opens the browser at the correct port
        /// If no instance is found it launches a new process
        /// </summary>
        [MenuItem("Window/Visualizer/Run")]
        public static void RunVisualizer()
        {
            if (!CheckIfVisualizerInstalled())
            {
                SetupVisualizer();
            }

            //The dataPath is used as a unique identifier for the project
            var project = Application.dataPath;

            var (pythonPid, port, visualizerPid) = ReadEntry(project);

            EditorUtility.DisplayProgressBar("Opening Visualizer", "Checking if instance exists", 0.5f / 4);

            //If there is a python instance for this project AND it is alive then just run browser
            if (pythonPid != -1 && ProcessAlive(pythonPid, port, visualizerPid))
            {
                EditorUtility.DisplayProgressBar("Opening Visualizer", "Opening", 4f / 4);
                LaunchBrowser(port);
                EditorUtility.ClearProgressBar();
            }

            //Otherwise delete any previous entry for this project and launch a new process
            else
            {
                DeleteEntry(project);
                var before = Process.GetProcesses();

                EditorUtility.DisplayProgressBar("Opening Visualizer", "Running executable", 1f / 4);
                var errorCode = ExecuteVisualizer();
                if (errorCode == -1)
                {
                    Debug.LogError("Could not launch visualizer tool");
                    EditorUtility.ClearProgressBar();
                    return;
                }

                Process[] after;

                const int maxAttempts = 10;

                //Poll for new processes until the visualizer process is launched
                EditorUtility.DisplayProgressBar("Opening Visualizer", "Finding Visualizer instance", 2f / 4);
                var newVisualizerPid = -1;
                var attempts = 0;
                while (newVisualizerPid == -1)
                {
                    Thread.Sleep(1000);
                    after = Process.GetProcesses();
                    newVisualizerPid = GetNewProcessID(before, after, k_NameOfVisualizerProcess);
                    if (attempts == maxAttempts)
                    {
                        Debug.LogError("Failed to get visualizer ID");
                        EditorUtility.ClearProgressBar();
                        return;
                    }

                    attempts++;
                }

                //Poll for new processes until the streamlit python script is launched
                EditorUtility.DisplayProgressBar("Opening Visualizer", "Finding Streamlit instance", 3f / 4);
                var newPythonPid = -1;
                attempts = 0;
                while (newPythonPid == -1)
                {
                    Thread.Sleep(1000);
                    after = Process.GetProcesses();
                    newPythonPid = GetNewProcessID(before, after, "python");
                    if (attempts == maxAttempts)
                    {
                        Debug.LogError("Failed to get python ID");
                        EditorUtility.ClearProgressBar();
                        return;
                    }

                    attempts++;
                }

                //Poll until the python script starts using the port
                EditorUtility.DisplayProgressBar("Opening Visualizer", "Finding Port", 3.5f / 4);
                var newPort = -1;
                attempts = 0;
                while (newPort == -1)
                {
                    Thread.Sleep(1000);
                    newPort = GetPortForPid(newPythonPid);
                    if (attempts == maxAttempts)
                    {
                        Debug.LogError("Failed to get PORT");
                        EditorUtility.ClearProgressBar();
                        return;
                    }

                    attempts++;
                }

                //Save this into the streamlit_instances.csv file
                WriteEntry(project, newPythonPid, newPort, newVisualizerPid);

                //When launching the process it will try to open a new tab in the default browser, however if a tab for it already exists it will not
                //For convenience if the user wants to force a new one to open they can press on "manually open"
                /*if (EditorUtility.DisplayDialog("Opening Visualizer Tool",
                    $"The visualizer tool should open shortly in your default browser at http://localhost:{newPort}.\n\nIf this is not the case after a few seconds you may open it manually",
                    "Manually Open",
                    "Cancel"))
                {
                    LaunchBrowser(newPort);
                }*/

                EditorUtility.DisplayProgressBar("Opening Visualizer", "Opening", 4f / 4);
                LaunchBrowser(newPort);
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Runs visualizer instance (streamlit) from the python for unity install
        /// </summary>
        static int ExecuteVisualizer()
        {
#if UNITY_EDITOR_WIN
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets", "/Library/PythonInstall/Scripts"));
#elif UNITY_EDITOR_OSX
            var packagesPath = Application.dataPath.Replace("/Assets","/Library/PythonInstall/bin");
#endif

            var pathToData = PlayerPrefs.GetString(SimulationState.latestOutputDirectoryKey);
#if UNITY_EDITOR_WIN
            packagesPath = packagesPath.Replace("/", "\\");
            pathToData = pathToData.Replace("/", "\\");
#endif

#if UNITY_EDITOR_WIN
            var command = $"cd \"{pathToData}\" && \"{packagesPath}\\datasetvisualizer.exe\" --data=\".\"";
#elif UNITY_EDITOR_OSX
            var command = $"cd \'{pathToData}\'; \'{packagesPath}/datasetvisualizer\' --data=\'.\'";
#endif

            string output = null;
            var exitCode = 0;
            var pid = ExecuteCmd(command, ref exitCode, ref output, waitForExit: 0, displayWindow: false);
            if (exitCode != 0)
            {
                Debug.LogError("Problem occured when launching the visualizer - Exit Code: " + exitCode);
                return -1;
            }

            return pid;
        }

        static (int pythonPID, int port, int visualizerPID) ReadEntry(string project)
        {
            if (!File.Exists(PathToStreamlitInstances))
                return (-1, -1, -1);

            using (var sr = File.OpenText(PathToStreamlitInstances))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var entry = line.TrimEnd().Split(',');
                    if (entry[0] == project)
                    {
                        //The -1 on ports is because the System.Diagnosis.Process API starts at 0 where as the PID in Windows and Mac start at 1
                        return (int.Parse(entry[1]) - 1, int.Parse(entry[2]), int.Parse(entry[3]) - 1);
                    }
                }
            }

            return (-1, -1, -1);
        }

        static void WriteEntry(string project, int pythonId, int port, int visualizerId)
        {
            var path = PathToStreamlitInstances;
            using (var sw = File.AppendText(path))
            {
                sw.WriteLine($"{project},{pythonId},{port},{visualizerId}");
            }
        }

        static void DeleteEntry(string project)
        {
            var path = PathToStreamlitInstances;
            if (!File.Exists(path))
                return;
            var entries = new List<string>(File.ReadAllLines(path));
            entries = entries.FindAll(x => !x.StartsWith(project));
            using (var sw = File.CreateText(path))
            {
                foreach (var entry in entries)
                {
                    sw.WriteLine(entry.TrimEnd());
                }
            }
        }

        /// <summary>
        /// Finds the process id of the first process that is different between the before and after array
        /// and that contains name
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static int GetNewProcessID(Process[] before, Process[] after, string name)
        {
            foreach (var p in after)
            {
                var isNew = true;

                // try/catch to skip any process that may:
                // not exist anymore/may be on another computer/are not associated with a living process
                try
                {
                    if (p.ProcessName.ToLower().Contains(name.ToLower()))
                    {
                        foreach (var q in before)
                        {
                            if (p.Id == q.Id)
                            {
                                isNew = false;
                                break;
                            }
                        }

                        if (isNew)
                        {
                            return p.Id;
                        }
                    }
                }
                catch(Exception ex)
                {
                    if (!(ex is InvalidOperationException || ex is NotSupportedException))
                    {
                        throw;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Finds which port the process PID is using
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        static int GetPortForPid(int pid)
        {
            foreach (var p in ProcessPorts.ProcessPortMap)
            {
                if (p.ProcessId == pid)
                {
                    return p.PortNumber;
                }
            }

            return -1;
        }

        /// <summary>
        /// Launches browser at localhost:port
        /// </summary>
        /// <param name="port"></param>
        static void LaunchBrowser(int port)
        {
            Process.Start($"http://localhost:{port}");
        }

        /// <summary>
        /// Check if streamlit process is alive
        /// </summary>
        /// <param name="pythonPid"></param>
        /// <param name="port"></param>
        /// <param name="visualizerPid"></param>
        /// <returns></returns>
        static bool ProcessAlive(int pythonPid, int port, int visualizerPid)
        {
            return PidExists(pythonPid) &&
                CheckProcessName(pythonPid, "python") &&
                ProcessListensToPort(pythonPid, port) &&
                PidExists(visualizerPid) &&
                CheckProcessName(visualizerPid, k_NameOfVisualizerProcess);
        }

        /// <summary>
        /// Check if a process with ProcessId = PID is alive
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        static bool PidExists(int pid)
        {
            try
            {
                var proc = Process.GetProcessById(pid + 1);
                if (proc.HasExited)
                {
                    return false;
                }
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if process with PID has a name that contains "name"
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static bool CheckProcessName(int pid, string name)
        {
            var proc = Process.GetProcessById(pid + 1);
            return proc.ProcessName.ToLower().Contains(name);
        }

        /// <summary>
        /// Check if the given PID listens to given port
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        static bool ProcessListensToPort(int pid, int port)
        {
            var processes = ProcessPorts.ProcessPortMap.FindAll(
                x => x.ProcessId == pid + 1 && x.PortNumber == port
            );
            return processes.Count >= 1;
        }

        /// <summary>
        /// Static class that returns the list of processes and the ports those processes use.
        /// </summary>
        static class ProcessPorts
        {
            /// <summary>
            /// A list of ProcessPorts that contain the mapping of processes and the ports that the process uses.
            /// </summary>
            public static List<ProcessPort> ProcessPortMap => GetNetStatPorts();

            /// <summary>
            /// This method distills the output from Windows: netstat -a -n -o or OSX: netstat -v -a into a list of ProcessPorts that provide a mapping between
            /// the process (name and id) and the ports that the process is using.
            /// </summary>
            /// <returns></returns>
            static List<ProcessPort> GetNetStatPorts()
            {
                var processPorts = new List<ProcessPort>();

                var startInfo = new ProcessStartInfo();
#if UNITY_EDITOR_WIN
                startInfo.FileName = "netstat.exe";
                startInfo.Arguments = "-a -n -o";
#elif UNITY_EDITOR_OSX
                startInfo.FileName = "netstat";
                startInfo.Arguments = "-n -v -a";
#endif
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;

                using (var proc = new Process())
                {
                    proc.StartInfo = startInfo;
                    proc.Start();
#if UNITY_EDITOR_OSX
                    proc.WaitForExit(2500);
#endif

                    var standardOutput = proc.StandardOutput;
                    var standardError = proc.StandardError;

                    var netStatContent = standardOutput.ReadToEnd() + standardError.ReadToEnd();
                    var netStatExitStatus = proc.ExitCode.ToString();

                    if (netStatExitStatus != "0")
                    {
                        Debug.LogError("NetStat command failed.   This may require elevated permissions.");
                    }

#if UNITY_EDITOR_WIN
                    var netStatRows = Regex.Split(netStatContent, "\r\n");
#elif UNITY_EDITOR_OSX
                        var netStatRows = Regex.Split(netStatContent, "\n");
#endif

                    foreach (var netStatRow in netStatRows)
                    {
                        var tokens = Regex.Split(netStatRow, "\\s+");
#if UNITY_EDITOR_WIN
                        if (tokens.Length > 4 && (tokens[1].Equals("UDP") || tokens[1].Equals("TCP")))
                        {
                            var ipAddress = Regex.Replace(tokens[2], @"\[(.*?)\]", "1.1.1.1");
                            try
                            {
                                processPorts.Add(new ProcessPort(
                                    tokens[1] == "UDP" ? Convert.ToInt32(tokens[4]) : Convert.ToInt32(tokens[5]),
                                    Convert.ToInt32(ipAddress.Split(':')[1])
                                ));
                            }
                            catch (Exception ex)
                            {
                                if (ex is IndexOutOfRangeException || ex is FormatException || ex is OverflowException)
                                {
                                    Debug.LogError("Could not convert the following NetStat row to a Process to Port mapping.");
                                    Debug.LogError(netStatRow);
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                        else
                        {
                            if (!netStatRow.Trim().StartsWith("Proto") && !netStatRow.Trim().StartsWith("Active") && !String.IsNullOrWhiteSpace(netStatRow))
                            {
                                Debug.LogError("Unrecognized NetStat row to a Process to Port mapping.");
                                Debug.LogError(netStatRow);
                            }
                        }
#elif UNITY_EDITOR_OSX
                            if (tokens.Length == 12 && tokens[0].Equals("tcp4") & (tokens[3].Contains("localhost") || tokens[3].Contains("*.")))
                            {
                                try
                                {
                                    if(tokens[5] != "CLOSED")
                                    {
                                        processPorts.Add(new ProcessPort(
                                            Convert.ToInt32(tokens[8]),
                                            Convert.ToInt32(tokens[3].Split('.')[1])
                                        ));
                                    }
                                }
                                catch (FormatException)
                                {
                                    //On mac rows show up in a difficult to predict order of formats (this skips all rows that we don't care about)
                                }
                                catch (Exception ex)
                                {
                                    if (ex is IndexOutOfRangeException || ex is OverflowException)
                                    {
                                        Debug.LogError("Could not convert the following NetStat row to a Process to Port mapping.");
                                        Debug.LogError(netStatRow);
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }
                            }
#endif
                    }
                }

                return processPorts;
            }
        }

        /// <summary>
        /// A mapping for processes to ports and ports to processes that are being used in the system.
        /// </summary>
        class ProcessPort
        {
            /// <summary>
            /// Internal constructor to initialize the mapping of process to port.
            /// </summary>
            /// <param name="processId"></param>
            /// <param name="portNumber"></param>
            internal ProcessPort(int processId, int portNumber)
            {
                ProcessId = processId;
                PortNumber = portNumber;
            }

            public int ProcessId { get; }

            public int PortNumber { get; }
        }

        [MenuItem("Window/Visualizer/Check For Updates")]
        static void CheckForUpdates()
        {
            if (!CheckIfVisualizerInstalled())
            {
                if (EditorUtility.DisplayDialog("Visualizer not Installed",
                    $"The visualizer is not yet installed, do you wish to install it?",
                    "Install",
                    "Cancel"))
                {
                    SetupVisualizer();
                }

                return;
            }

            var latestVersion = Task.Run(PipAPI.GetLatestVersionNumber).Result;

#if UNITY_EDITOR_WIN
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets", "/Library/PythonInstall/Scripts"));
#elif UNITY_EDITOR_OSX
            string packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets","/Library/PythonInstall/bin"));
#endif

#if UNITY_EDITOR_WIN
            packagesPath = packagesPath.Replace("/", "\\");
#endif
            var exitCode = -1;
            string output = null;
#if UNITY_EDITOR_WIN
            ExecuteCmd($"\"{packagesPath}\"\\pip3.bat show unity-cv-datasetvisualizer", ref exitCode, ref output, waitForExit: 1500, getOutput: true);
#elif UNITY_EDITOR_OSX
            ExecuteCmd($"cd \'{packagesPath}\'; ./python3.7 -m pip show unity-cv-datasetvisualizer", ref exitCode, ref output, waitForExit: 1500, getOutput: true);
#endif
            if (exitCode != 0)
            {
                Debug.LogError("Could not get the version of the current install of the visualizer tool");
                return;
            }

            var outputLines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var currentVersion = (from t in outputLines where t.StartsWith("Version: ") select t.Substring("Version: ".Length)).FirstOrDefault();

            if (currentVersion == null)
            {
                Debug.LogError("Could not parse the version of the current install of the visualizer tool");
                return;
            }

            if (PipAPI.CompareVersions(latestVersion, currentVersion) > 0)
            {
                if (EditorUtility.DisplayDialog("Update Found for Visualizer",
                    $"An update was found for the Visualizer",
                    "Install",
                    "Cancel"))
                {
                    SetupVisualizer();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Visualizer", "No new updates found", "close");
            }
        }

        static bool CheckIfVisualizerInstalled()
        {
#if UNITY_EDITOR_WIN
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets", "/Library/PythonInstall/Scripts"));
#elif UNITY_EDITOR_OSX
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets","/Library/PythonInstall/bin"));
#endif

#if UNITY_EDITOR_WIN
            packagesPath = packagesPath.Replace("/", "\\");
#endif
            var exitCode = 0;
            string output = null;
#if UNITY_EDITOR_WIN
            ExecuteCmd($"\"{packagesPath}\"\\pip3.bat list", ref exitCode, ref output, waitForExit: 1500, getOutput: true);
#elif UNITY_EDITOR_OSX
            ExecuteCmd($"cd \'{packagesPath}\'; ./python3.7 -m pip list", ref exitCode, ref output, waitForExit: 1500, getOutput: true);
#endif
            if (exitCode != 0)
            {
                Debug.LogError("Could not list pip packages");
                return false;
            }

            var outputLines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            return outputLines.Any(t => t.StartsWith("unity-cv-datasetvisualizer"));
        }
    }
}
#endif
