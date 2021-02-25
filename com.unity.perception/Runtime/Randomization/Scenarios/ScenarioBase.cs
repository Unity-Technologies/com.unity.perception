using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios.Serialization;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// Derive ScenarioBase to implement a custom scenario
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public abstract class ScenarioBase : MonoBehaviour
    {
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
        /// Overwrites this scenario's randomizer settings and scenario constants from a JSON serialized configuration
        /// </summary>
        /// <param name="json">The JSON string to deserialize</param>
        public virtual void DeserializeFromJson(string json)
        {
            ScenarioSerializer.Deserialize(this, json);
        }

        /// <summary>
        /// Overwrites this scenario's randomizer settings and scenario constants using a configuration file located at
        /// the provided file path
        /// </summary>
        /// <param name="configFilePath">The file path to the configuration file to deserialize</param>
        public virtual void DeserializeFromFile(string configFilePath)
        {
            if (string.IsNullOrEmpty(configFilePath))
                throw new ArgumentException($"{nameof(configFilePath)} is null or empty");
            if (!File.Exists(configFilePath))
                throw new ArgumentException($"No configuration file found at {configFilePath}");

            var jsonText = File.ReadAllText(configFilePath);
            DeserializeFromJson(jsonText);
#if !UNITY_EDITOR
            Debug.Log($"Deserialized scenario configuration from {Path.GetFullPath(configFilePath)}");
#endif
        }

        /// <summary>
        /// Deserialize scenario settings from a file passed through a command line argument
        /// </summary>
        /// <param name="commandLineArg">The command line argument to look for</param>
        protected virtual void DeserializeFromCommandLine(string commandLineArg="--scenario-config-file")
        {
            var args = Environment.GetCommandLineArgs();
            var filePath = string.Empty;
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "--scenario-config-file")
                    continue;
                filePath = args[i + 1];
                break;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log("No --scenario-config-file command line arg specified. " +
                    "Proceeding with editor assigned scenario configuration values.");
                return;
            }

            try { DeserializeFromFile(filePath); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("An exception was caught while attempting to parse a " +
                    $"scenario configuration file at {filePath}. Cleaning up and exiting simulation.");
            }
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
        /// OnAwake is called when this scenario MonoBehaviour is created or instantiated
        /// </summary>
        protected virtual void OnAwake() { }

        /// <summary>
        /// OnConfigurationImport is called before OnStart in the same frame. This method by default loads a scenario
        /// settings from a file before the scenario begins.
        /// </summary>
        protected virtual void OnConfigurationImport()
        {
#if !UNITY_EDITOR
            DeserializeFromCommandLine();
#endif
        }

        /// <summary>
        /// OnStart is called when the scenario first begins playing
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// OnIterationStart is called before a new iteration begins
        /// </summary>
        protected virtual void OnIterationStart() { }

        /// <summary>
        /// OnIterationStart is called after each iteration has completed
        /// </summary>
        protected virtual void OnIterationEnd() { }

        /// <summary>
        /// OnUpdate is called every frame while the scenario is playing
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// OnComplete is called when this scenario's isScenarioComplete property
        /// returns true during its main update loop
        /// </summary>
        protected virtual void OnComplete() { }

        /// <summary>
        /// OnIdle is called each frame after the scenario has completed
        /// </summary>
        protected virtual void OnIdle() { }

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
            OnAwake();
            foreach (var randomizer in m_Randomizers)
                randomizer.Awake();
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

        void Update()
        {
            switch (state)
            {
                case State.Initializing:
                    if (isScenarioReadyToStart)
                    {
                        OnConfigurationImport();
                        state = State.Playing;
                        OnStart();
                        foreach (var randomizer in m_Randomizers)
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

        void IterationLoop()
        {
            // Increment iteration and cleanup last iteration
            if (isIterationComplete)
            {
                IncrementIteration();
                currentIterationFrame = 0;
                foreach (var randomizer in activeRandomizers)
                    randomizer.IterationEnd();
                OnIterationEnd();
            }

            // Quit if scenario is complete
            if (isScenarioComplete)
            {
                foreach (var randomizer in activeRandomizers)
                    randomizer.ScenarioComplete();
                OnComplete();
                state = State.Idle;
                OnIdle();
                return;
            }

            // Perform new iteration tasks
            if (currentIterationFrame == 0)
            {
                ResetRandomStateOnIteration();
                OnIterationStart();
                foreach (var randomizer in activeRandomizers)
                    randomizer.IterationStart();
            }

            // Perform new frame tasks
            OnUpdate();
            foreach (var randomizer in activeRandomizers)
                randomizer.Update();

            // Iterate scenario frame count
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
                throw new ScenarioException("Randomizers cannot be added to the scenario after it has started");
            m_Randomizers.RemoveAt(index);
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
            Initializing,
            Playing,
            Idle
        }
    }
}
