using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

public class VisualizerInstaller : EditorWindow
{

    //This files stores entries as ProjectDataPath,PythonPID,Port,VisualizerPID
    //It keeps a record of the instances of visualizer opened so that we don't open a new one everytime
    private static readonly string _filenameStreamlitInstances = "Unity/streamlit_instances.csv";
    private static string pathToStreamlitInstances
    {
        get
        {
#if UNITY_EDITOR_WIN
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _filenameStreamlitInstances);
#elif UNITY_EDITOR_OSX
            return Path.Combine(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library"
                        ),
                    _filenameStreamlitInstances);
#endif
        }
    }
    private static readonly string nameOfVisualizerProcess
#if UNITY_EDITOR_WIN
        = "datasetvisualizer";
#elif UNITY_EDITOR_OSX
        = "bash";
#else
        = "";
#endif

    

    /// <summary>
    /// Install visualizer (Assumes python3 and pip3 are already installed)
    /// - installs virtualenv if it is not already installed
    /// - and setups a virtual environment for visualizer
    /// </summary>
    [MenuItem("Window/Visualizer/Setup")]
    static void SetupVisualizer()
    { 
        string project = Application.dataPath;

        (int pythonPID, int port, int visualizerPID) = ReadEntry(project);

        //If there is a python instance for this project AND it is alive then setup will fail (must kill instance)
        if(pythonPID != -1 && ProcessAlive(pythonPID, port, visualizerPID))
        {
            if (EditorUtility.DisplayDialog("Kill visualizer?",
                "The visualizer tool can't be running while you setup, would you like to kill the current instance?",
                "Kill visualizer",
                "Cancel"))
            {
                Process.GetProcessById(pythonPID + 1).Kill();
            }
            else
            {
                return;
            }
        }
        

        int steps = 3;
        int ExitCode = 0;

        //==============================SETUP PATHS======================================
#if UNITY_EDITOR_WIN
        string packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets","/Library/PythonInstall/Scripts"));
#elif UNITY_EDITOR_OSX
        string packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets","/Library/PythonInstall/bin"));
#endif

#if UNITY_EDITOR_WIN
        packagesPath = packagesPath.Replace("/", "\\");
#endif

        
        //==============================INSTALL VISUALIZER IN PYTHON FOR UNITY======================================

        EditorUtility.DisplayProgressBar("Setting up the Visualizer", "Installing the Visualizer...", 2.5f / steps);
#if UNITY_EDITOR_WIN
        ExecuteCMD($"\"{packagesPath}\"\\pip3.bat install --no-warn-script-location unity-cv-datasetvisualizer", ref ExitCode);
#elif UNITY_EDITOR_OSX
        ExecuteCMD($"cd \'{packagesPath}\'; ./python3.7 -m pip install unity-cv-datasetvisualizer", ref ExitCode);
#endif
        if (ExitCode != 0) {
            EditorUtility.ClearProgressBar();
            return;
        }

        EditorUtility.ClearProgressBar();

        PlayerPrefs.SetInt("VisualizerSetup", 1);
    }

    /// <summary>
    /// Executes command in cmd or console depending on system
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="waitForExit">Should it wait for exit before returning to the editor (i.e. is it not async?)</param>
    /// <param name="displayWindow">Should the command window be displayed</param>
    /// <returns></returns>
    private static int ExecuteCMD(string command, ref int ExitCode, bool waitForExit = true, bool displayWindow = false, bool redirectOutput = false)
    {
        string shell = "";
        string argument = "";
        string output = "";

#if UNITY_EDITOR_WIN
        shell = "cmd.exe";
        argument = $"/c \"{command}\"";
#elif UNITY_EDITOR_OSX
        shell = "/bin/bash";
        argument = $"-c \"{command}\"";
#endif

        UnityEngine.Debug.Log(command);

        ProcessStartInfo info = new ProcessStartInfo(shell, argument);

        info.CreateNoWindow = !displayWindow;
        info.UseShellExecute = false;
        info.RedirectStandardOutput = false;
        info.RedirectStandardError = waitForExit;

        Process cmd = Process.Start(info);

        if (!waitForExit)
        {
            return cmd.Id;
        }


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

        return -1;
    }
    /// <summary>
    /// If an instance is already running for this project it opens the browser at the correct port
    /// If no instance is found it launches a new process
    /// </summary>
    [MenuItem("Window/Visualizer/Run")]
    private static void RunVisualizer()
    {
        if (!PlayerPrefs.HasKey("VisualizerSetup") || PlayerPrefs.GetInt("VisualizerSetup") != 1)
        {
            SetupVisualizer();
        }

        //The dataPath is used as a unique identifier for the project
        string project = Application.dataPath;

        (int pythonPID, int port, int visualizerPID) = ReadEntry(project);

        //If there is a python instance for this project AND it is alive then just run browser
        if(pythonPID != -1 && ProcessAlive(pythonPID, port, visualizerPID))
        {
            LaunchBrowser(port);            
        }
        //Otherwise delete any previous entry for this project and launch a new process
        else
        {
            DeleteEntry(project); 
            Process[] before = Process.GetProcesses();

            int errorCode = LaunchVisualizer();
            if(errorCode == -1)
            {
                UnityEngine.Debug.LogError("Could not launch visualizer tool");
                return;
            }
            Process[] after = null;

            int maxAttempts = 5;
            //Poll for new processes until the visualizer process is launched
            int newVisualizerPID = -1;
            int attempts = 0;
            while(newVisualizerPID == -1)
            {
                Thread.Sleep(1000);
                after = Process.GetProcesses();
                newVisualizerPID = GetNewProcessID(before, after, nameOfVisualizerProcess);
                if(attempts == maxAttempts)
                {
                    UnityEngine.Debug.LogError("Failed to get visualizer ID");
                    return;
                }
                attempts++;
            }

            //Poll for new processes until the streamlit python script is launched
            int newPythonPID = -1;
            attempts = 0;
            while(newPythonPID == -1)
            {
                Thread.Sleep(1000);
                after = Process.GetProcesses();
                newPythonPID = GetNewProcessID(before, after, "python");
                if(attempts == maxAttempts)
                {
                    UnityEngine.Debug.LogError("Failed to get python ID");
                    return;
                }
                attempts++;
            }

            //Poll until the python script starts using the port
            int newPort = -1;
            attempts = 0;
            while(newPort == -1)
            {
                Thread.Sleep(1000);
                newPort = GetPortForPID(newPythonPID);
                if(attempts == maxAttempts)
                {
                    UnityEngine.Debug.LogError("Failed to get PORT");
                    return;
                }
                attempts++;
            }

            //Save this into the streamlit_instances.csv file
            WriteEntry(project, newPythonPID, newPort, newVisualizerPID);

            //When launching the process it will try to open a new tab in the default browser, however if a tab for it already exists it will not
            //For convinience if the user wants to force a new one to open they can press on "manually open"
            /*if (EditorUtility.DisplayDialog("Opening Visualizer Tool",
                $"The visualizer tool should open shortly in your default browser at http://localhost:{newPort}.\n\nIf this is not the case after a few seconds you may open it manually",
                "Manually Open",
                "Cancel"))
            {
                LaunchBrowser(newPort);
            }*/

            LaunchBrowser(newPort);
            
        }
    }

    /// <summary>
    /// Runs visualizer instance (streamlit) from the python for unity install
    /// </summary>
    static int LaunchVisualizer()
    {
        string path = Path.GetFullPath(Application.dataPath.Replace("/Assets", ""));
#if UNITY_EDITOR_WIN
        string packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets","/Library/PythonInstall/Scripts"));
#elif UNITY_EDITOR_OSX
        string packagesPath = Application.dataPath.Replace("/Assets","/Library/PythonInstall/bin");
#endif

        string pathToData = PlayerPrefs.GetString(SimulationState.latestOutputDirectoryKey);
#if UNITY_EDITOR_WIN
        path = path.Replace("/", "\\");
        packagesPath = packagesPath.Replace("/", "\\");
        pathToData = pathToData.Replace("/", "\\");
#endif
        string command = "";
#if UNITY_EDITOR_WIN
        command = $"cd \"{pathToData}\" && \"{packagesPath}\\datasetvisualizer.exe\" preview --data=\".\"";
#elif UNITY_EDITOR_OSX
        command = $"cd \'{pathToData}\'; \'{packagesPath}/datasetvisualizer\' preview --data=\'.\'";
#endif

        UnityEngine.Debug.Log(pathToData);

        UnityEngine.Debug.Log(command);

        int ExitCode = 0;
        int PID = ExecuteCMD(command, ref ExitCode, waitForExit: false, displayWindow: false);
        if (ExitCode != 0)
        {
            UnityEngine.Debug.LogError("Problem occured when launching the visualizer - Exit Code: " + ExitCode);
            return -1;
        }
        return PID;
    }

    private static (int pythonPID, int port, int visualizerPID) ReadEntry(string project)
    {
        string path = pathToStreamlitInstances;
        if (!Directory.Exists(pathToStreamlitInstances))
        if (!File.Exists(path))
            return (-1,-1,-1);
        using (StreamReader sr = File.OpenText(path))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] entry = line.TrimEnd().Split(',');
                if(entry[0] == project)
                {
                    //The -1 on ports is because the System.Diagnosis.Process API starts at 0 where as the PID in Windows and Mac start at 1
                    return (int.Parse(entry[1]) -1, int.Parse(entry[2]), int.Parse(entry[3]) -1);
                }
            }
        }
        return (-1,-1,-1);
    }

    private static void WriteEntry(string project, int pythonId, int port, int visualizerId)
    {
        string path = pathToStreamlitInstances;
        using (StreamWriter sw = File.AppendText(path))
        {
            sw.WriteLine($"{project},{pythonId},{port},{visualizerId}");
        }
    }

    private static void DeleteEntry(string project)
    {
        string path = pathToStreamlitInstances;
        if (!File.Exists(path))
            return;
        List<string> entries = new List<string>(File.ReadAllLines(path));
        entries = entries.FindAll(x => !x.StartsWith(project));
        using(StreamWriter sw = File.CreateText(path))
        {
            foreach(string entry in entries)
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
    private static int GetNewProcessID(Process[] before, Process[] after, string name)
    {
        foreach(Process p in after)
        {
            bool isNew = true;
            // try/catch to skip any process that may not exist anymore
            try
            {
                if (p.ProcessName.ToLower().Contains(name))
                {
                    foreach (Process q in before)
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
            catch { }
        }
        return -1;
    }

    /// <summary>
    /// Finds which port the process PID is using
    /// </summary>
    /// <param name="PID"></param>
    /// <returns></returns>
    private static int GetPortForPID(int PID)
    {
        foreach(ProcessPort p in ProcessPorts.ProcessPortMap)
        {
            if(p.ProcessId == PID)
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
    private static void LaunchBrowser(int port)
    {
        Process.Start($"http://localhost:{port}");
    }

    /// <summary>
    /// Check if streamlit process is alive
    /// </summary>
    /// <param name="pythonPID"></param>
    /// <param name="port"></param>
    /// <param name="visualizerPID"></param>
    /// <returns></returns>
    private static bool ProcessAlive(int pythonPID, int port, int visualizerPID)
    {
        return PIDExists(pythonPID) &&
            checkProcessName(pythonPID, "python") &&
            ProcessListensToPort(pythonPID, port) &&
            PIDExists(visualizerPID) &&
            checkProcessName(visualizerPID, nameOfVisualizerProcess);
    }

    /// <summary>
    /// Check if a process with ProcessId = PID is alive
    /// </summary>
    /// <param name="PID"></param>
    /// <returns></returns>
    private static bool PIDExists(int PID)
    {
         try
         {
            Process proc = Process.GetProcessById(PID + 1);
            if (proc.HasExited)
            {
                return false;
            }
            else
            {  
                return true;
            }
         }
         catch (ArgumentException)
         {
            return false;
         }
    }

    /// <summary>
    /// Check if process with PID has a name that contains "name"
    /// </summary>
    /// <param name="PID"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private static bool checkProcessName(int PID, string name)
    {
        Process proc = Process.GetProcessById(PID + 1);
        return proc.ProcessName.ToLower().Contains(name);
    }

    /// <summary>
    /// Check if the given PID listens to given port
    /// </summary>
    /// <param name="PID"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    private static bool ProcessListensToPort(int PID, int port)
    {
        List<ProcessPort> processes = ProcessPorts.ProcessPortMap.FindAll(
            x => x.ProcessId == PID + 1 && x.PortNumber == port
        );
        return processes.Count >= 1;
    }

    /// <summary>
    /// Static class that returns the list of processes and the ports those processes use.
    /// </summary>
    private static class ProcessPorts
    {
        /// <summary>
        /// A list of ProcesesPorts that contain the mapping of processes and the ports that the process uses.
        /// </summary>
        public static List<ProcessPort> ProcessPortMap
        {
            get
            {
                return GetNetStatPorts();
            }
        }


        /// <summary>
        /// This method distills the output from netstat -a -n -o into a list of ProcessPorts that provide a mapping between
        /// the process (name and id) and the ports that the process is using.
        /// </summary>
        /// <returns></returns>
        private static List<ProcessPort> GetNetStatPorts()
        {
            List<ProcessPort> ProcessPorts = new List<ProcessPort>();

            try
            {
                using (Process Proc = new Process())
                {

                    ProcessStartInfo StartInfo = new ProcessStartInfo();
#if UNITY_EDITOR_WIN
                    StartInfo.FileName = "netstat.exe";
                    StartInfo.Arguments = "-a -n -o";
#elif UNITY_EDITOR_OSX
                    StartInfo.FileName = "netstat";
                    StartInfo.Arguments = "-v -a";
#endif
                    StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    StartInfo.UseShellExecute = false;
                    StartInfo.RedirectStandardInput = true;
                    StartInfo.RedirectStandardOutput = true;
                    StartInfo.RedirectStandardError = true;

                    Proc.StartInfo = StartInfo;
                    Proc.Start();
#if UNITY_EDITOR_OSX
                    Proc.WaitForExit();
#endif

                    StreamReader StandardOutput = Proc.StandardOutput;
                    StreamReader StandardError = Proc.StandardError;

                    string NetStatContent = StandardOutput.ReadToEnd() + StandardError.ReadToEnd();
                    string NetStatExitStatus = Proc.ExitCode.ToString();

                    if (NetStatExitStatus != "0")
                    {
                        UnityEngine.Debug.LogError("NetStat command failed.   This may require elevated permissions.");
                    }

                    string[] NetStatRows = null;
#if UNITY_EDITOR_WIN
                    NetStatRows = Regex.Split(NetStatContent, "\r\n");
#elif UNITY_EDITOR_OSX
                    NetStatRows = Regex.Split(NetStatContent, "\n");
#endif

                    foreach (string NetStatRow in NetStatRows)
                    {
                        string[] Tokens = Regex.Split(NetStatRow, "\\s+");
#if UNITY_EDITOR_WIN
                        if (Tokens.Length > 4 && (Tokens[1].Equals("UDP") || Tokens[1].Equals("TCP")))
                        {
                            string IpAddress = Regex.Replace(Tokens[2], @"\[(.*?)\]", "1.1.1.1");
                            try
                            {
                                ProcessPorts.Add(new ProcessPort(
                                    Tokens[1] == "UDP" ? GetProcessName(Convert.ToInt16(Tokens[4])) : GetProcessName(Convert.ToInt32(Tokens[5])),
                                    Tokens[1] == "UDP" ? Convert.ToInt32(Tokens[4]) : Convert.ToInt32(Tokens[5]),
                                    IpAddress.Contains("1.1.1.1") ? String.Format("{0}v6", Tokens[1]) : String.Format("{0}v4", Tokens[1]),
                                    Convert.ToInt32(IpAddress.Split(':')[1])
                                ));
                            }
                            catch
                            {
                                UnityEngine.Debug.LogError("Could not convert the following NetStat row to a Process to Port mapping.");
                                UnityEngine.Debug.LogError(NetStatRow);
                            }
                        }
                        else
                        {
                            if (!NetStatRow.Trim().StartsWith("Proto") && !NetStatRow.Trim().StartsWith("Active") && !String.IsNullOrWhiteSpace(NetStatRow))
                            {
                                UnityEngine.Debug.LogError("Unrecognized NetStat row to a Process to Port mapping.");
                                UnityEngine.Debug.LogError(NetStatRow);
                            }
                        }
#elif UNITY_EDITOR_OSX
                        if (Tokens.Length == 12 && Tokens[0].Equals("tcp4") & (Tokens[3].Contains("localhost") || Tokens[3].Contains("*.")))
                        {
                            try
                            {
                                if(Tokens[5] != "CLOSED"){
                                    ProcessPorts.Add(new ProcessPort(
                                        GetProcessName(Convert.ToInt32(Tokens[8])),
                                        Convert.ToInt32(Tokens[8]),
                                        "tcp4",
                                        Convert.ToInt32(Tokens[3].Split('.')[1])
                                    ));
                                }
                            }
                            catch
                            {
                                UnityEngine.Debug.LogError("Could not convert the following NetStat row to a Process to Port mapping.");
                                UnityEngine.Debug.LogError(NetStatRow);
                            }
                        }
#endif
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex.Message);
            }
            return ProcessPorts;
        }

        /// <summary>
        /// Private method that handles pulling the process name (if one exists) from the process id.
        /// </summary>
        /// <param name="ProcessId"></param>
        /// <returns></returns>
        private static string GetProcessName(int ProcessId)
        {
            string procName = "UNKNOWN";

            try
            {
                procName = Process.GetProcessById(ProcessId).ProcessName;
            }
            catch { }

            return procName;
        }
    }

    /// <summary>
    /// A mapping for processes to ports and ports to processes that are being used in the system.
    /// </summary>
    private class ProcessPort
    {
        private string _ProcessName = String.Empty;
        private int _ProcessId = 0;
        private string _Protocol = String.Empty;
        private int _PortNumber = 0;

        /// <summary>
        /// Internal constructor to initialize the mapping of process to port.
        /// </summary>
        /// <param name="ProcessName">Name of process to be </param>
        /// <param name="ProcessId"></param>
        /// <param name="Protocol"></param>
        /// <param name="PortNumber"></param>
        internal ProcessPort (string ProcessName, int ProcessId, string Protocol, int PortNumber)
        {
            _ProcessName = ProcessName;
            _ProcessId = ProcessId;
            _Protocol = Protocol;
            _PortNumber = PortNumber;
        }

        public string ProcessPortDescription
        {
            get
            {
                return String.Format("{0} ({1} port {2} pid {3})", _ProcessName, _Protocol, _PortNumber, _ProcessId);
            }
        }
        public string ProcessName
        {
            get { return _ProcessName; }
        }
        public int ProcessId
        {
            get { return _ProcessId; }
        }
        public string Protocol
        {
            get { return _Protocol; }
        }
        public int PortNumber
        {
            get { return _PortNumber; }
        }
    }
}
