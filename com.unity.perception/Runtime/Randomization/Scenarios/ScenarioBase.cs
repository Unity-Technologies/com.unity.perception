using System;
using System.Collections.Generic;
using System.IO;
using Unity.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
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
        const string k_ScenarioIterationMetricDefinitionId = "DB1B258E-D1D0-41B6-8751-16F601A2E230";
        static ScenarioBase s_ActiveScenario;
        MetricDefinition m_IterationMetricDefinition;

        /// <summary>
        /// The list of randomizers managed by this scenario
        /// </summary>
        [SerializeReference] protected List<Randomizer> m_Randomizers = new List<Randomizer>();

        /// <summary>
        /// On some platforms, the simulation capture package cannot capture the first frame of output,
        /// so this field is used to track whether the first frame has been skipped yet.
        /// </summary>
        protected bool m_SkipFrame = true;

        /// <summary>
        /// Setting this field to true will cause the scenario to enter an idle state. By default, scenarios will enter
        /// the idle state after its isScenarioComplete property has returned true.
        /// </summary>
        protected bool m_Idle;

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
        /// Returns the active parameter scenario in the scene
        /// </summary>
        public static ScenarioBase activeScenario
        {
            get
            {
#if UNITY_EDITOR

                // This compiler define is required to allow samplers to
                // iterate the scenario's random state in edit-mode
                if (s_ActiveScenario == null)
                    s_ActiveScenario = FindObjectOfType<ScenarioBase>();
#endif
                return s_ActiveScenario;
            }
            private set
            {
                if (value != null && s_ActiveScenario != null && value != s_ActiveScenario)
                    throw new ScenarioException("There cannot be more than one active Scenario");
                s_ActiveScenario = value;
            }
        }

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
        /// Returns whether the current scenario iteration has completed
        /// </summary>
        public abstract bool isIterationComplete { get; }

        /// <summary>
        /// Returns whether the entire scenario has completed
        /// </summary>
        public abstract bool isScenarioComplete { get; }

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

            var absolutePath = Path.GetFullPath(configFilePath);
#if UNITY_EDITOR
            Debug.Log($"Deserialized scenario configuration from {absolutePath}. " +
                "Using undo in the editor will revert these changes to your scenario.");
#else
            Debug.Log($"Deserialized scenario configuration from {absolutePath}");
#endif
        }

        /// <summary>
        /// Resets SamplerState.randomState with a new seed value generated by hashing this Scenario's randomSeed
        /// with its currentIteration
        /// </summary>
        protected virtual void ResetRandomStateOnIteration()
        {
            SamplerState.randomState = SamplerUtility.IterateSeed((uint)currentIteration, genericConstants.randomSeed);
        }

        /// <summary>
        /// OnAwake is called right after this scenario MonoBehaviour is created or instantiated
        /// </summary>
        protected virtual void OnAwake() { }

        /// <summary>
        /// OnStart is called after Awake but before the first Update method call
        /// </summary>
        protected virtual void OnStart()
        {
#if !UNITY_EDITOR
            var args = Environment.GetCommandLineArgs();
            var filePath = string.Empty;
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--scenario-config-file")
                {
                    filePath = args[i + 1];
                    break;
                }
            }

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log("No --scenario-config-file command line arg specified. " +
                    "Proceeding with editor assigned scenario configuration values.");
                return;
            }

            try
            {
                DeserializeFromFile(filePath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("An exception was caught while attempting to parse a " +
                    $"scenario configuration file at {filePath}. Cleaning up and exiting simulation.");
                m_Idle = true;
            }
#endif
        }

        /// <summary>
        /// OnComplete is called when this scenario's isScenarioComplete property
        /// returns true during its main update loop
        /// </summary>
        protected virtual void OnComplete()
        {
            DatasetCapture.ResetSimulation();
            m_Idle = true;
        }

        /// <summary>
        /// OnIdle is called each frame after the scenario has completed
        /// </summary>
        protected virtual void OnIdle()
        {
            Manager.Instance.Shutdown();
            if (!Manager.FinalUploadsDone)
                return;
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
        }

        void Awake()
        {
            activeScenario = this;
            foreach (var randomizer in m_Randomizers)
                randomizer.Create();
            ValidateParameters();
            m_IterationMetricDefinition = DatasetCapture.RegisterMetricDefinition(
                "scenario_iteration", "Iteration information for dataset sequences",
                Guid.Parse(k_ScenarioIterationMetricDefinitionId));
            OnAwake();
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

        void Start()
        {
            var randomSeedMetricDefinition = DatasetCapture.RegisterMetricDefinition(
                "random-seed",
                "The random seed used to initialize the random state of the simulation. Only triggered once per simulation.",
                Guid.Parse("14adb394-46c0-47e8-a3f0-99e754483b76"));
            DatasetCapture.ReportMetric(randomSeedMetricDefinition, new[] { genericConstants.randomSeed });
            OnStart();
        }

        void Update()
        {
            // TODO: remove this check when the perception camera can capture the first frame of output
            if (m_SkipFrame)
            {
                m_SkipFrame = false;
                return;
            }

            // Wait for any final uploads before exiting quitting
            if (m_Idle)
            {
                OnIdle();
                return;
            }

            // Increment iteration and cleanup last iteration
            if (isIterationComplete)
            {
                IncrementIteration();
                currentIterationFrame = 0;
                foreach (var randomizer in activeRandomizers)
                    randomizer.IterationEnd();
            }

            // Quit if scenario is complete
            if (isScenarioComplete)
            {
                foreach (var randomizer in activeRandomizers)
                    randomizer.ScenarioComplete();
                OnComplete();
            }

            // Perform new iteration tasks
            if (currentIterationFrame == 0)
            {
                DatasetCapture.StartNewSequence();
                ResetRandomStateOnIteration();
                DatasetCapture.ReportMetric(m_IterationMetricDefinition, new[]
                {
                    new IterationMetricData { iteration = currentIteration }
                });
                foreach (var randomizer in activeRandomizers)
                    randomizer.IterationStart();
            }

            // Perform new frame tasks
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
        /// <param name="newRandomizer"></param>
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
            foreach (var randomizer in m_Randomizers)
                if (randomizer.GetType() == newRandomizer.GetType())
                    throw new ScenarioException(
                        $"Cannot add another randomizer of type ${newRandomizer.GetType()} when " +
                        $"a scenario of this type is already present in the scenario");
            m_Randomizers.Insert(index, newRandomizer);
#if UNITY_EDITOR
            if (Application.isPlaying)
                newRandomizer.Create();
#else
            newRandomizer.Create();
#endif
        }

        /// <summary>
        /// Remove the randomizer present at the given index
        /// </summary>
        /// <param name="index">The index of the randomizer to remove</param>
        public void RemoveRandomizerAt(int index)
        {
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
                try
                {
                    parameter.Validate();
                }
                catch (ParameterValidationException exception)
                {
                    Debug.LogException(exception, this);
                }
        }

        struct IterationMetricData
        {
            // ReSharper disable once NotAccessedField.Local
            public int iteration;
        }
    }
}
