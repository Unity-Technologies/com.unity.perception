using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios.Serialization;
using UnityEngine.Perception.Utilities;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// Derive ScenarioBase to implement a custom scenario
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public abstract class ScenarioBase : MonoBehaviour
    {
        const string k_RestoreCrashCommandLineKey = "-restoreCrash";

        static ScenarioBase s_ActiveScenario;

        /// <summary>
        /// Returns the active parameter scenario in the scene
        /// </summary>
        public static ScenarioBase activeScenario
        {
            get => s_ActiveScenario;
            private set
            {
                if (value != null && s_ActiveScenario != null && value != s_ActiveScenario)
                    throw new ScenarioException("There cannot be more than one active Scenario");
                s_ActiveScenario = value;
            }
        }

        /// <summary>
        /// The current activity state of the scenario
        /// </summary>
        public State state { get; private set; } = State.Initializing;

        /// <summary>
        /// The list of randomizers managed by this scenario
        /// </summary>
        [SerializeReference] List<Randomizer> m_Randomizers = new List<Randomizer>();

        /// <summary>
        /// An external text asset that is loaded when the scenario starts to configure scenario settings
        /// </summary>
        public TextAsset configuration;

        private bool m_ShouldRestartIteration;
        private bool m_ShouldDelayIteration;

        private const int k_MaxIterationStartCount = 100;

        /// <summary>
        /// Enumerates over all enabled randomizers
        /// </summary>
        public IEnumerable<Randomizer> activeRandomizers
        {
            get
            {
                foreach (var randomizer in m_Randomizers)
                    if (randomizer.enabled)
                        yield return randomizer;
            }
        }

        /// <summary>
        /// Return the list of randomizers attached to this scenario
        /// </summary>
        public IReadOnlyList<Randomizer> randomizers => m_Randomizers.AsReadOnly();

        /// <summary>
        /// Returns this scenario's non-typed serialized constants
        /// </summary>
        public abstract ScenarioConstants genericConstants { get; }

        /// <summary>
        /// The number of frames that have elapsed since the current scenario iteration was Setup
        /// </summary>
        public int currentIterationFrame { get; private set; }

        /// <summary>
        /// The number of frames that have elapsed since the scenario was initialized
        /// </summary>
        public int framesSinceInitialization { get; private set; }

        /// <summary>
        /// The current iteration index of the scenario
        /// </summary>
        public int currentIteration { get; protected set; }

        /// <summary>
        /// The scenario will begin on the frame this property first returns true
        /// </summary>
        /// <returns>Whether the scenario should start this frame</returns>
        protected abstract bool isScenarioReadyToStart { get; }

        /// <summary>
        /// Returns whether the current scenario iteration has completed
        /// </summary>
        protected abstract bool isIterationComplete { get; }

        /// <summary>
        /// Returns whether the scenario has completed
        /// </summary>
        protected abstract bool isScenarioComplete { get; }

        /// <summary>
        /// This method selects what the next iteration index will be. By default, the scenario will simply progress to
        /// the next iteration, but this behaviour can be overriden.
        /// </summary>
        protected virtual void IncrementIteration()
        {
            currentIteration++;
        }

        /// <summary>
        /// Serializes the scenario's constants and randomizer settings to a JSON string
        /// </summary>
        /// <returns>The scenario configuration as a JSON string</returns>
        public virtual string SerializeToJson()
        {
            return ScenarioSerializer.SerializeToJsonString(this);
        }

        /// <summary>
        /// Serializes the scenario's constants and randomizer settings to a JSON file located at the path resolved by
        /// the defaultConfigFilePath scenario property
        /// </summary>
        /// <param name="filePath">The file path to serialize the scenario to</param>
        public virtual void SerializeToFile(string filePath)
        {
            ScenarioSerializer.SerializeToFile(this, filePath);
        }

        /// <summary>
        /// Loads a scenario configuration from a file located at the given file path
        /// </summary>
        /// <param name="filePath">The file path of the scenario configuration file</param>
        /// <exception cref="FileNotFoundException"></exception>
        public void LoadConfigurationFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"No configuration file found at {filePath}");
            var jsonText = File.ReadAllText(filePath);

            Debug.Log($"Configuration file data: {jsonText}");

            configuration = new TextAsset(jsonText);
        }

        /// <summary>
        /// Deserialize scenario settings from a file passed through a command line argument
        /// </summary>
        /// <param name="commandLineArg">The command line argument to look for</param>
        void LoadConfigurationFromCommandLine(string commandLineArg = "--scenario-config-file")
        {
            var filePath = string.Empty;

            Debug.Log($"application args {string.Join(" ", Environment.GetCommandLineArgs())}");
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.Contains(commandLineArg))
                {
                    var split = arg.Split('=');
                    if (split.Length != 2)
                    {
                        throw new Exception($"Invalid configuration {string.Join(" ", Environment.GetCommandLineArgs())}");
                    }

                    filePath = split[1];
                    break;
                }
            }

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log($"No {commandLineArg} command line arg specified. " +
                    "Proceeding with editor assigned scenario configuration values.");
                return;
            }

            LoadConfigurationFromFile(filePath);
        }

        /// <summary>
        /// Loads and stores a JSON scenario settings configuration file before the scenario starts
        /// </summary>
        protected virtual void LoadConfigurationAsset()
        {
            configuration = null;

#if UNITY_SIMULATION_CORE_PRESENT
            if (Unity.Simulation.Configuration.Instance.IsSimulationRunningInCloud())
            {
                var filePath = new Uri(Unity.Simulation.Configuration.Instance.SimulationConfig.app_param_uri).LocalPath;
                LoadConfigurationFromFile(filePath);
                return;
            }
#endif
            LoadConfigurationFromCommandLine();
        }

        /// <summary>
        /// Overwrites this scenario's randomizer settings and scenario constants from a JSON serialized configuration
        /// </summary>
        protected virtual void DeserializeConfiguration()
        {
            if (configuration != null)
                ScenarioSerializer.Deserialize(this, configuration.text);
        }

        internal void DeserializeConfigurationInternal()
        {
            DeserializeConfiguration();
        }

        /// <summary>
        /// Resets SamplerState.randomState with a new seed value generated by hashing this Scenario's randomSeed
        /// with its currentIteration
        /// </summary>
        protected virtual void ResetRandomStateOnIteration()
        {
            SamplerState.randomState = SamplerUtility.IterateSeed((uint)currentIteration, genericConstants.randomSeed);
        }

        #region LifecycleHooks

        /// <summary>
        /// Tries to restore previous simulation state
        /// </summary>
        protected virtual void OnResumeSimulation() {}

        /// <summary>
        /// OnAwake is called when this scenario MonoBehaviour is created or instantiated
        /// </summary>
        protected virtual void OnAwake() {}

        /// <summary>
        /// OnStart is called when the scenario first begins playing
        /// </summary>
        protected virtual void OnStart() {}

        /// <summary>
        /// OnIterationStart is called before a new iteration begins
        /// </summary>
        protected virtual void OnIterationStart() {}

        /// <summary>
        /// OnIterationStart is called after each iteration has completed
        /// </summary>
        protected virtual void OnIterationEnd() {}

        /// <summary>
        /// OnUpdate is called every frame while the scenario is playing
        /// </summary>
        protected virtual void OnUpdate() {}

        /// <summary>
        /// OnComplete is called when this scenario's isScenarioComplete property
        /// returns true during its main update loop
        /// </summary>
        protected virtual void OnComplete() {}

        /// <summary>
        /// OnIdle is called each frame after the scenario has completed
        /// </summary>
        protected virtual void OnIdle() {}

        /// <summary>
        /// Restart the scenario
        /// </summary>
        public void Restart()
        {
            if (state != State.Idle)
                throw new ScenarioException(
                    "A Scenario cannot be restarted until it is finished and has entered the Idle state");
            currentIteration = 0;
            currentIterationFrame = 0;
            framesSinceInitialization = 0;
            state = State.Initializing;
        }

        /// <summary>
        /// Exit to playmode if in the Editor or quit the application if in a built player
        /// </summary>
        protected void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        #endregion


        #region MonoBehaviourMethods
        void Awake()
        {
            activeScenario = this;
            try
            {
#if !UNITY_EDITOR
                LoadConfigurationAsset();
#endif
                DeserializeConfiguration();
            }
            catch (Exception e)
            {
                QuitApplication(e);
            }

            ValidateCrashRestore();

            try
            {
                OnAwake();
            }
            catch (Exception e)
            {
                QuitApplication(e);
            }

            foreach (var randomizer in activeRandomizers)
            {
                try
                {
                    randomizer.Awake();
                }
                catch (Exception e)
                {
                    QuitApplication(e);
                }
            }

            ValidateParameters();
        }

        /// <summary>
        /// OnEnable is called when this scenario is enabled
        /// </summary>
        protected virtual void OnEnable()
        {
            activeScenario = this;
        }

        /// <summary>
        /// OnEnable is called when this scenario is disabled
        /// </summary>
        protected virtual void OnDisable()
        {
            activeScenario = null;
        }

        /// <summary>
        /// OnDestroy is called before this scenario destroyed
        /// </summary>
        protected void OnDestroy()
        {
            foreach (var randomizer in activeRandomizers)
            {
                try
                {
                    randomizer.Destroy();
                }
                catch (Exception e)
                {
                    Debug.LogError($"OnDestroy randomizer {randomizer.GetType()} thrown an exception {e}");
                }
            }
        }

        internal virtual void Update()
        {
            switch (state)
            {
                case State.Initializing:
                    if (isScenarioReadyToStart)
                    {
                        state = State.Playing;
                        OnStart();
                        foreach (var randomizer in activeRandomizers)
                            randomizer.ScenarioStart();
                        IterationLoop();
                    }
                    break;

                case State.Playing:
                    IterationLoop();
                    break;

                case State.Idle:
                    OnIdle();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        $"Invalid state {state} encountered while updating scenario");
            }
        }

        #endregion

        static void QuitApplication(Exception e)
        {
            Debug.LogException(e);
            // save any game data here
#if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            EditorApplication.isPlaying = false;
#else
            Environment.ExitCode = 1;
            Application.Quit(1);
            Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.Abort);
#endif
        }

        void IterationLoop()
        {
            // Increment iteration and cleanup last iteration
            if (isIterationComplete)
            {
                try
                {
                    IncrementIteration();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[IncrementIteration] scenario {GetType()} thrown an exception {e.Message}");
                    QuitApplication(e);
                }

                currentIterationFrame = 0;
                foreach (var randomizer in activeRandomizers)
                {
                    try
                    {
                        randomizer.IterationEnd();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[IterationEnd] randomizer {randomizer.GetType()} thrown an exception {e.Message}");
                        QuitApplication(e);
                    }
                }

                try
                {
                    OnIterationEnd();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[OnIterationEnd] scenario {GetType()} thrown an exception {e.Message}");
                    QuitApplication(e);
                }
            }

            // Quit if scenario is complete
            if (isScenarioComplete)
            {
                foreach (var randomizer in activeRandomizers)
                {
                    try
                    {
                        randomizer.ScenarioComplete();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ScenarioComplete] randomizer {randomizer.GetType()} thrown an exception {e.Message}");
                        QuitApplication(e);
                    }
                }

                try
                {
                    OnComplete();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[OnComplete] scenario {GetType()} thrown an exception {e.Message}");
                    QuitApplication(e);
                }

                try
                {
                    state = State.Idle;
                    OnIdle();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[OnIdle] scenario {GetType()} thrown an exception {e.Message}");
                    QuitApplication(e);
                }
                return;
            }

            // Perform new iteration tasks
            if (currentIterationFrame == 0)
            {
                ResetRandomStateOnIteration();
                try
                {
                    OnIterationStart();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[OnIterationStart] scenario {GetType()} thrown an exception {e.Message}");
                    QuitApplication(e);
                }

                int iterationStartCount = 0;
                do
                {
                    m_ShouldRestartIteration = false;
                    m_ShouldDelayIteration = false;
                    iterationStartCount++;
                    foreach (var randomizer in activeRandomizers)
                    {
                        try
                        {
                            randomizer.IterationStart();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[IterationStart] randomizer {randomizer.GetType()} thrown an exception {e.Message}");
                            QuitApplication(e);
                        }

                        if (m_ShouldRestartIteration)
                            break;

                        if (m_ShouldDelayIteration)
                        {
                            Debug.Log($"Iteration was delayed by {randomizer.GetType().Name}");
                            break;
                        }
                    }
                    if (m_ShouldDelayIteration)
                        break;
                }
                while (m_ShouldRestartIteration && iterationStartCount < k_MaxIterationStartCount);

                if (m_ShouldRestartIteration)
                {
                    Debug.LogError($"The iteration was restarted {k_MaxIterationStartCount} times. Continuing the scenario to prevent an infinite loop.");
                    m_ShouldRestartIteration = false;
                }
            }

            // Perform new frame tasks
            try
            {
                OnUpdate();
            }
            catch (Exception e)
            {
                Debug.LogError($"[OnUpdate] scenario {GetType()} thrown an exception {e.Message}");
                QuitApplication(e);
            }

            foreach (var randomizer in activeRandomizers)
            {
                try
                {
                    randomizer.Update();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[OnUpdate] randomizer {randomizer.GetType()} thrown an exception {e.Message}");
                    QuitApplication(e);
                }
            }

            // Iterate scenario frame count
            if (!m_ShouldDelayIteration)
                currentIterationFrame++;

            framesSinceInitialization++;
        }

        /// <summary>
        /// Called by the "Add Randomizer" button in the scenario Inspector
        /// </summary>
        /// <param name="randomizerType">The type of randomizer to create</param>
        /// <returns>The newly created randomizer</returns>
        /// <exception cref="ScenarioException"></exception>
        internal Randomizer CreateRandomizer(Type randomizerType)
        {
            if (!randomizerType.IsSubclassOf(typeof(Randomizer)))
                throw new ScenarioException(
                    $"Cannot add non-randomizer type {randomizerType.Name} to randomizer list");
            var newRandomizer = (Randomizer)Activator.CreateInstance(randomizerType);
            AddRandomizer(newRandomizer);
            return newRandomizer;
        }

        /// <summary>
        /// Append a randomizer to the end of the randomizer list
        /// </summary>
        /// <param name="newRandomizer">The Randomizer to add to the Scenario</param>
        public void AddRandomizer(Randomizer newRandomizer)
        {
            InsertRandomizer(m_Randomizers.Count, newRandomizer);
        }

        /// <summary>
        /// Insert a randomizer at a given index within the randomizer list
        /// </summary>
        /// <param name="index">The index to place the randomizer</param>
        /// <param name="newRandomizer">The randomizer to add to the list</param>
        /// <exception cref="ScenarioException"></exception>
        public void InsertRandomizer(int index, Randomizer newRandomizer)
        {
            if (state != State.Initializing)
                throw new ScenarioException("Randomizers cannot be added to the scenario after it has started");
            foreach (var randomizer in m_Randomizers)
                if (randomizer.GetType() == newRandomizer.GetType())
                    throw new ScenarioException(
                        $"Cannot add another randomizer of type ${newRandomizer.GetType()} when " +
                        $"a scenario of this type is already present in the scenario");
            m_Randomizers.Insert(index, newRandomizer);
#if UNITY_EDITOR
            if (Application.isPlaying)
                newRandomizer.Awake();
#else
            newRandomizer.Awake();
#endif
        }

        /// <summary>
        /// Remove the randomizer present at the given index
        /// </summary>
        /// <param name="index">The index of the randomizer to remove</param>
        public void RemoveRandomizerAt(int index)
        {
            if (state != State.Initializing)
                throw new ScenarioException("Randomizers cannot be removed from the scenario after it has started");
            m_Randomizers.RemoveAt(index);
        }

        internal void ClearNullRandomizers()
        {
            if (state != State.Initializing)
                throw new ScenarioException("Randomizers cannot be removed from the scenario after it has started");
            m_Randomizers.RemoveAll(x => x == null);
        }

        /// <summary>
        /// Returns the randomizer present at the given index
        /// </summary>
        /// <param name="index">The lookup index</param>
        /// <returns>The randomizer present at the given index</returns>
        public Randomizer GetRandomizer(int index)
        {
            return m_Randomizers[index];
        }

        /// <summary>
        /// Finds and returns a randomizer attached to this scenario of the specified Randomizer type
        /// </summary>
        /// <typeparam name="T">The type of randomizer to find</typeparam>
        /// <returns>A randomizer of the specified type</returns>
        /// <exception cref="ScenarioException"></exception>
        public T GetRandomizer<T>() where T : Randomizer
        {
            foreach (var randomizer in m_Randomizers)
                if (randomizer is T typedRandomizer)
                    return typedRandomizer;
            throw new ScenarioException($"A Randomizer of type {typeof(T).Name} was not added to this scenario");
        }

        void ValidateParameters()
        {
            foreach (var randomizer in m_Randomizers)
                foreach (var parameter in randomizer.parameters)
                {
                    try { parameter.Validate(); }
                    catch (ParameterValidationException exception) { Debug.LogException(exception, this); }
                }
        }

        /// <summary>
        /// Enum used to track the lifecycle of a Scenario
        /// </summary>
        public enum State
        {
            /// <summary>
            /// The scenario has yet to start
            /// </summary>
            Initializing,

            /// <summary>
            /// The scenario is executing
            /// </summary>
            Playing,

            /// <summary>
            /// The scenario has finished and is idle
            /// </summary>
            Idle
        }

        /// <summary>
        /// Set a flag, that current simulation should be restarted
        /// </summary>
        public void RestartIteration()
        {
            m_ShouldRestartIteration = true;
        }

        /// <summary>
        /// Delays the current iteration by one frame.
        /// This results in <see cref="OnIterationStart" /> being called again for the same iteration.
        /// </summary>
        /// <remarks>
        /// Must be called from within the <see cref="OnIterationStart"/> function of a class
        /// inheriting from <see cref="Randomizer" />.
        /// </remarks>
        public void DelayIteration()
        {
            m_ShouldDelayIteration = true;
        }

        /// <summary>
        /// Try to restore previous simulation
        /// </summary>
        void ValidateCrashRestore()
        {
            if (!ExecutionTools.HasCommandLineArgumentValue(k_RestoreCrashCommandLineKey))
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            try
            {
                OnResumeSimulation();
            }
            catch (Exception e)
            {
                QuitApplication(e);
            }
        }
    }
}
