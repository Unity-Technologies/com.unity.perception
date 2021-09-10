#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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


        static Task InstallationCommand(ref int exitCode, string packagesPath)
        {
            var exitCodeCopy = exitCode;
#if UNITY_EDITOR_WIN
            var task = Task.Run(() => ExecuteCmd($"\"{packagesPath}\"\\pip3.bat install --upgrade --no-warn-script-location unity-cv-datasetvisualizer", ref exitCodeCopy));
#elif UNITY_EDITOR_OSX
            var task = Task.Run(() => ExecuteCmd($"cd \'{packagesPath}\'; ./python3.7 -m pip install --upgrade unity-cv-datasetvisualizer", ref exitCodeCopy));
#endif
            exitCode = exitCodeCopy;
            return task;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Install visualizer (Uses the python3.7 installation that comes with python for unity)
        /// </summary>
        static async Task SetupVisualizer()
        {
            var project = Application.dataPath;

            var (pythonPid, port, visualizerPid) = ReadEntry(project);

            //If there is a python instance for this project AND it is alive then setup will fail (must kill instance)
            if (pythonPid != -1 && ProcessAlive(pythonPid, port, visualizerPid))
            {
                if (EditorUtility.DisplayDialog("Shutdown visualizer?",
                    "The visualizer tool can't be running while you re-installs, would you like to shutdown the current instance?",
                    "Yes",
                    "No"))
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

            //==============================SETUP PATHS======================================
#if UNITY_EDITOR_WIN
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets", "/Library/PythonInstall/Scripts"));
#elif UNITY_EDITOR_OSX
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets","/Library/PythonInstall/bin"));
#endif

#if UNITY_EDITOR_WIN
            packagesPath = packagesPath.Replace("/", "\\");
#endif

            //==============================INSTALL VISUALIZER IN PYTHON FOR UNITY======================================

            EditorUtility.DisplayProgressBar("Setting up the Visualizer", "Installing Visualizer (This may take a few minutes)", 2f / steps);

            await InstallationCommand(ref exitCode, packagesPath);

            if (exitCode != 0)
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            EditorUtility.ClearProgressBar();
            Debug.Log("Successfully installed visualizer tool!");
        }

        /// <summary>
        /// Executes command in cmd or console depending on system
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="exitCode">int reference to get exit code</param>
        /// <param name="waitForExit">Whether this method should wait for the command to exist</param>
        static void ExecuteCmd(string command, ref int exitCode, bool waitForExit = true)
        {
#if UNITY_EDITOR_WIN
            const string shell = "cmd.exe";
            var argument = $"/c \"{command}\"";
#elif UNITY_EDITOR_OSX
            const string shell = "/bin/bash";
            var argument = $"-c \"{command}\"";
#endif

            var info = new ProcessStartInfo(shell, argument);

            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = false;
            info.RedirectStandardError = false;

            var cmd = Process.Start(info);
            if (cmd == null)
            {
                Debug.LogError($"Could not create process using command {command}");
                return;
            }

            if (waitForExit)
            {
                cmd.WaitForExit();
                exitCode = cmd.ExitCode;
            }
        }

        /// <summary>
        /// Executes command in cmd or console depending on system and waits a specified amount for the execution to finish
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="exitCode">int reference to get exit code</param>
        /// <param name="output">string reference to save output</param>
        /// <param name="waitForExit">Time to wait for the command to exit before returning to the editor</param>
        static void ExecuteCmd(string command, ref int exitCode, ref string output, int waitForExit)
        {
#if UNITY_EDITOR_WIN
            const string shell = "cmd.exe";
            var argument = $"/c \"{command}\"";
#elif UNITY_EDITOR_OSX
            const string shell = "/bin/bash";
            var argument = $"-c \"{command}\"";
#endif

            var info = new ProcessStartInfo(shell, argument);

            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            var cmd = Process.Start(info);
            if (cmd == null)
            {
                Debug.LogError($"Could not create process using command {command}");
                return;
            }

            cmd.WaitForExit(waitForExit);
            output = cmd.StandardOutput.ReadToEnd();

            if (cmd.HasExited)
            {
                exitCode = cmd.ExitCode;
                if (exitCode != 0)
                {
                    Debug.LogError($"Error - {exitCode} - Failed to execute: {command} - {cmd.StandardError.ReadToEnd()}");
                }
            }

            cmd.Close();
        }

        [MenuItem("Window/Dataset Visualizer/Open")]
        public static async Task RunVisualizerButton()
        {
            var project = Application.dataPath;
            await RunVisualizer(project);
        }
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// If an instance is already running for this project it opens the browser at the correct port
        /// If no instance is found it launches a new process
        /// </summary>
        public static async Task RunVisualizer(string project)
        {
            try
            {
                if (!CheckIfVisualizerInstalled(project))
                {
                    await SetupVisualizer();
                }

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
                    ExecuteVisualizer(project);

                    Process[] after;

                    const int maxAttempts = 10;

                    //Poll for new processes until the visualizer process is launched
                    EditorUtility.DisplayProgressBar("Opening Visualizer", "Finding Visualizer instance", 2f / 4);
                    var newVisualizerPid = -1;
                    var attempts = 0;
                    while (newVisualizerPid == -1)
                    {
                        await Task.Delay(1000);
                        after = Process.GetProcesses();
                        newVisualizerPid = GetNewProcessID(before, after, k_NameOfVisualizerProcess);
                        if (attempts == maxAttempts)
                        {
                            Debug.LogWarning("Failed to get visualizer process ID after launch. Note that this does not necessarily mean the tool did not launch successfully. However, running the visualizer again will now create a new instance of the process.");
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
                        await Task.Delay(1000);
                        after = Process.GetProcesses();
                        newPythonPid = GetNewProcessID(before, after, "python");
                        if (attempts == maxAttempts)
                        {
                            Debug.LogWarning("Failed to get python process ID after launch. Note that this does not necessarily mean the tool did not launch successfully. However, running the visualizer again will now create a new instance of the process.");
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
                        await Task.Delay(1000);
                        newPort = GetPortForPid(newPythonPid);
                        if (attempts == maxAttempts)
                        {
                            Debug.LogWarning("Failed to get port number used by the visualizer tool after launch. Note that this does not necessarily mean the tool did not launch successfully. However, running the visualizer again will now create a new instance of the process.");
                            EditorUtility.ClearProgressBar();
                            return;
                        }

                        attempts++;
                    }

                    //Save this into the streamlit_instances.csv file
                    WriteEntry(project, newPythonPid, newPort, newVisualizerPid);

                    EditorUtility.DisplayProgressBar("Opening Visualizer", "Opening", 4f / 4);

                    //When launching the process it will try to open a new tab in the default browser, however if a tab for it already exists it will not
                    //For convenience if the user wants to force a new one to open they can press on "manually open"
                    if (EditorUtility.DisplayDialog("Opening Visualizer Tool",
                        $"The visualizer tool should open shortly in your default browser at http://localhost:{newPort}.\n\nIf this is not the case after a few seconds you may open it manually",
                        "Manually Open",
                        "Cancel"))
                    {
                        LaunchBrowser(newPort);
                    }

                    EditorUtility.ClearProgressBar();
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Runs visualizer instance (streamlit) from the python for unity install
        /// </summary>
        static void ExecuteVisualizer(string project)
        {
#if UNITY_EDITOR_WIN
            var packagesPath = Path.GetFullPath(project.Replace("/Assets", "/Library/PythonInstall/Scripts"));
#elif UNITY_EDITOR_OSX
            var packagesPath = project.Replace("/Assets","/Library/PythonInstall/bin");
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

            var exitCode = 0;
            ExecuteCmd(command, ref exitCode, false);
            if (exitCode != 0)
            {
                Debug.LogError("Failed launching the visualizer - Exit Code: " + exitCode);
            }
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
            var processPortMap = PortProcessor.ProcessPortMap;
            if (processPortMap == null)
            {
                return -1;
            }

            foreach (var p in processPortMap.Where(p => p.ProcessId == pid))
            {
                return p.PortNumber;
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
                return !proc.HasExited;
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
            var processes = PortProcessor.ProcessPortMap.FindAll(
                x => x.ProcessId == pid + 1 && x.PortNumber == port
            );
            return processes.Count >= 1;
        }

        /// <summary>
        /// Static class that returns the list of processes and the ports those processes use.
        /// </summary>
        static class PortProcessor
        {
            /// <summary>
            /// A list of ProcessPorts that contain the mapping of processes and the ports that the process uses.
            /// </summary>
            public static List<ProcessPort> ProcessPortMap => GetNetStatPorts();

            /// <summary>
            /// This method distills the output from Windows: netstat -a -n -o or OSX: netstat -n -v -a into a list of ProcessPorts that provide a mapping between
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
                        Debug.LogError($"NetStat command failed. Try running '{startInfo.FileName} {startInfo.Arguments}' manually to verify that you have permissions to execute this command.");
                        return null;
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
                                    Debug.LogError("Could not convert the following NetStat row to a process to port mapping.");
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
                                Debug.LogError("Could not convert the following NetStat row to a process to port mapping.");
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
                                        Debug.LogError("Could not convert the following NetStat row to a process to port mapping.");
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
        readonly struct ProcessPort
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

        [MenuItem("Window/Dataset Visualizer/Check For Updates")]
        internal static async Task CheckForUpdates()
        {
            var project = Application.dataPath;
            if (!CheckIfVisualizerInstalled(project))
            {
                if (EditorUtility.DisplayDialog("Visualizer Not Installed",
                    $"The visualizer is not yet installed, do you wish to install it?",
                    "Install",
                    "Cancel"))
                {
                    await SetupVisualizer();
                }

                return;
            }

            var latestVersion = await PipAPI.GetLatestVersionNumber();

#if UNITY_EDITOR_WIN
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets", "/Library/PythonInstall/Scripts"));
#elif UNITY_EDITOR_OSX
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets","/Library/PythonInstall/bin"));
#endif

#if UNITY_EDITOR_WIN
            packagesPath = packagesPath.Replace("/", "\\");
#endif
            var exitCode = -1;
            string output = null;
#if UNITY_EDITOR_WIN
            ExecuteCmd($"\"{packagesPath}\"\\pip3.bat show unity-cv-datasetvisualizer", ref exitCode, ref output, 1500);
#elif UNITY_EDITOR_OSX
            ExecuteCmd($"cd \'{packagesPath}\'; ./python3.7 -m pip show unity-cv-datasetvisualizer", ref exitCode, ref output, 1500);
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
                Debug.LogError("Could not parse the version of the currently installed visualizer tool. Cancelling update check.");
                return;
            }

            if (PipAPI.CompareVersions(latestVersion, currentVersion) > 0)
            {
                if (EditorUtility.DisplayDialog("New Version Found!",
                    $"An update was found for the Visualizer",
                    "Install",
                    "Cancel"))
                {
                    await SetupVisualizer();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Visualizer", "No new updates found", "OK");
            }
        }

        static bool CheckIfVisualizerInstalled(string project)
        {
#if UNITY_EDITOR_WIN
            var packagesPath = Path.GetFullPath(project.Replace("/Assets", "/Library/PythonInstall/Scripts"));
#elif UNITY_EDITOR_OSX
            var packagesPath = Path.GetFullPath(project.Replace("/Assets","/Library/PythonInstall/bin"));
#endif

#if UNITY_EDITOR_WIN
            packagesPath = packagesPath.Replace("/", "\\");
#endif
            var exitCode = 0;
            string output = null;
#if UNITY_EDITOR_WIN
            ExecuteCmd($"\"{packagesPath}\"\\pip3.bat list", ref exitCode, ref output, waitForExit: 1500);
#elif UNITY_EDITOR_OSX
            ExecuteCmd($"cd \'{packagesPath}\'; ./python3.7 -m pip list", ref exitCode, ref output, waitForExit: 1500);
#endif
            if (exitCode != 0)
            {
                Debug.LogError("Could not list pip packages to check whether the visualizer is installed. Cancelling update check");
                return false;
            }

            var outputLines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            return outputLines.Any(t => t.StartsWith("unity-cv-datasetvisualizer"));
        }
    }
}
#endif
