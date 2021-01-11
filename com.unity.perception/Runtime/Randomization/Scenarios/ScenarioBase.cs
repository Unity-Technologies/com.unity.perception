using System;
using System.Collections.Generic;
using System.IO;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers;
using UnityEngine.Experimental.Perception.Randomization.Samplers;
using UnityEngine.Perception.GroundTruth;

namespace UnityEngine.Experimental.Perception.Randomization.Scenarios
{
    /// <summary>
    /// Derive ScenarioBase to implement a custom scenario
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public abstract class ScenarioBase : MonoBehaviour
    {
        static ScenarioBase s_ActiveScenario;

        uint m_RandomState = SamplerUtility.largePrime;
        bool m_SkipFrame = true;
        bool m_FirstScenarioFrame = true;
        bool m_WaitingForFinalUploads;

        IEnumerable<Randomizer> activeRandomizers
        {
            get
            {
                foreach (var randomizer in m_Randomizers)
                    if (randomizer.enabled)
                        yield return randomizer;
            }
        }

        // ReSharper disable once InconsistentNaming
        [SerializeReference] internal List<Randomizer> m_Randomizers = new List<Randomizer>();

        /// <summary>
        /// Return the list of randomizers attached to this scenario
        /// </summary>
        public IReadOnlyList<Randomizer> randomizers => m_Randomizers.AsReadOnly();

        /// <summary>
        /// If true, this scenario will quit the Unity application when it's finished executing
        /// </summary>
        [HideInInspector] public bool quitOnComplete = true;

        /// <summary>
        /// The random state of the scenario
        /// </summary>
        public uint randomState => m_RandomState;

        /// <summary>
        /// The name of the Json file this scenario's constants are serialized to/from.
        /// </summary>
        public virtual string configFileName => "scenario_configuration";

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
        /// Returns the asset location of the JSON serialized configuration.
        /// This API is used for finding the config file using the AssetDatabase API.
        /// </summary>
        public string defaultConfigFileAssetPath =>
            "Assets/StreamingAssets/" + configFileName + ".json";

        /// <summary>
        /// Returns the absolute file path of the JSON serialized configuration
        /// </summary>
        public string defaultConfigFilePath =>
            Application.dataPath + "/StreamingAssets/" + configFileName + ".json";

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
        /// Progresses the current scenario iteration
        /// </summary>
        protected virtual void IncrementIteration()
        {
            currentIteration++;
        }

        /// <summary>
        /// Serializes the scenario's constants and randomizer settings to a JSON string
        /// </summary>
        /// <returns>The scenario configuration as a JSON string</returns>
        public abstract string SerializeToJson();

        /// <summary>
        /// Serializes the scenario's constants and randomizer settings to a JSON file located at the path resolved by
        /// the defaultConfigFilePath scenario property
        /// </summary>
        public void SerializeToFile()
        {
            Directory.CreateDirectory(Application.dataPath + "/StreamingAssets/");
            using (var writer = new StreamWriter(defaultConfigFilePath, false))
                writer.Write(SerializeToJson());
        }

        /// <summary>
        /// Overwrites this scenario's randomizer settings and scenario constants from a JSON serialized configuration
        /// </summary>
        /// <param name="json">The JSON string to deserialize</param>
        public abstract void DeserializeFromJson(string json);

        /// <summary>
        /// Overwrites this scenario's randomizer settings and scenario constants using a configuration file located at
        /// the provided file path
        /// </summary>
        /// <param name="configFilePath">The file path to the configuration file to deserialize</param>
        public abstract void DeserializeFromFile(string configFilePath);

        /// <summary>
        /// Overwrites this scenario's randomizer settings and scenario constants using a configuration file located at
        /// this scenario's defaultConfigFilePath
        /// </summary>
        public void DeserializeFromFile()
        {
            DeserializeFromFile(defaultConfigFilePath);
        }

        /// <summary>
        /// This method executed directly after this scenario has been registered and initialized
        /// </summary>
        protected virtual void OnAwake() { }

        void Awake()
        {
            activeScenario = this;
            OnAwake();
            foreach (var randomizer in m_Randomizers)
                randomizer.Create();
            ValidateParameters();

            // Don't skip the first frame if executing on Unity Simulation
            if (Configuration.Instance.IsSimulationRunningInCloud())
                m_SkipFrame = false;
        }

        void OnEnable()
        {
            activeScenario = this;
        }

        void OnDisable()
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
#if !UNITY_EDITOR
            if (File.Exists(defaultConfigFilePath))
                DeserializeFromFile();
            else
                Debug.Log($"No configuration file found at {defaultConfigFilePath}. " +
                    "Proceeding with built in scenario constants and randomizer settings.");
#endif
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
            if (m_WaitingForFinalUploads && quitOnComplete)
            {
                Manager.Instance.Shutdown();
                if (!Manager.FinalUploadsDone)
                    return;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
                return;
            }

            // Iterate Scenario
            if (m_FirstScenarioFrame)
            {
                m_FirstScenarioFrame = false;
            }
            else
            {
                currentIterationFrame++;
                framesSinceInitialization++;
                if (isIterationComplete)
                {
                    IncrementIteration();
                    currentIterationFrame = 0;
                    foreach (var randomizer in activeRandomizers)
                        randomizer.IterationEnd();
                }
            }

            // Quit if scenario is complete
            if (isScenarioComplete)
            {
                foreach (var randomizer in activeRandomizers)
                    randomizer.ScenarioComplete();
                DatasetCapture.ResetSimulation();
                m_WaitingForFinalUploads = true;
                return;
            }

            // Perform new iteration tasks
            if (currentIterationFrame == 0)
            {
                DatasetCapture.StartNewSequence();
                m_RandomState = SamplerUtility.IterateSeed((uint)currentIteration, genericConstants.randomSeed);
                foreach (var randomizer in activeRandomizers)
                    randomizer.IterationStart();
            }

            // Perform new frame tasks
            foreach (var randomizer in activeRandomizers)
                randomizer.Update();
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

        /// <summary>
        /// Creates a new randomizer and adds it to this scenario
        /// </summary>
        /// <typeparam name="T">The type of randomizer to create</typeparam>
        /// <returns>The newly created randomizer</returns>
        public T CreateRandomizer<T>() where T : Randomizer, new()
        {
            return (T)CreateRandomizer(typeof(T));
        }

        internal Randomizer CreateRandomizer(Type randomizerType)
        {
            if (!randomizerType.IsSubclassOf(typeof(Randomizer)))
                throw new ScenarioException(
                    $"Cannot add non-randomizer type {randomizerType.Name} to randomizer list");
            foreach (var randomizer in m_Randomizers)
                if (randomizer.GetType() == randomizerType)
                    throw new ScenarioException(
                        $"Two Randomizers of the same type ({randomizerType.Name}) cannot both be active simultaneously");
            var newRandomizer = (Randomizer)Activator.CreateInstance(randomizerType);
            m_Randomizers.Add(newRandomizer);
#if UNITY_EDITOR
            if (Application.isPlaying)
                newRandomizer.Create();
#else
            newRandomizer.Create();
#endif
            return newRandomizer;
        }

        /// <summary>
        /// Removes a randomizer of the specified type from this scenario
        /// </summary>
        /// <typeparam name="T">The type of scenario to remove</typeparam>
        public void RemoveRandomizer<T>() where T : Randomizer, new()
        {
            RemoveRandomizer(typeof(T));
        }

        internal void RemoveRandomizer(Type randomizerType)
        {
            if (!randomizerType.IsSubclassOf(typeof(Randomizer)))
                throw new ScenarioException(
                    $"Cannot add non-randomizer type {randomizerType.Name} to randomizer list");
            var removed = false;
            for (var i = 0; i < m_Randomizers.Count; i++)
            {
                if (m_Randomizers[i].GetType() == randomizerType)
                {
                    m_Randomizers.RemoveAt(i);
                    removed = true;
                    break;
                }
            }
            if (!removed)
                throw new ScenarioException(
                    $"No active Randomizer of type {randomizerType.Name} could be removed");
        }

        /// <summary>
        /// Returns the execution order index of a randomizer of the given type
        /// </summary>
        /// <typeparam name="T">The type of randomizer to index</typeparam>
        /// <returns>The randomizer index</returns>
        /// <exception cref="ScenarioException"></exception>
        public int GetRandomizerIndex<T>() where T : Randomizer, new()
        {
            for (var i = 0; i < m_Randomizers.Count; i++)
            {
                var randomizer = m_Randomizers[i];
                if (randomizer is T)
                    return i;
            }
            throw new ScenarioException($"A Randomizer of type {typeof(T).Name} was not added to this scenario");
        }

        /// <summary>
        /// Moves a randomizer from one index to another
        /// </summary>
        /// <param name="currentIndex">The index of the randomizer to move</param>
        /// <param name="nextIndex">The index to move the randomizer to</param>
        public void ReorderRandomizer(int currentIndex, int nextIndex)
        {
            if (currentIndex == nextIndex)
                return;

            if (nextIndex > currentIndex)
                nextIndex--;

            var randomizer = m_Randomizers[currentIndex];
            m_Randomizers.RemoveAt(currentIndex);
            m_Randomizers.Insert(nextIndex, randomizer);
        }

        /// <summary>
        /// Generates a new random state and overwrites the old random state with the newly generated value
        /// </summary>
        /// <returns>The newly generated random state</returns>
        public uint NextRandomState()
        {
            m_RandomState = SamplerUtility.Hash32NonZero(m_RandomState);
            return m_RandomState;
        }

        void ValidateParameters()
        {
            foreach (var randomizer in m_Randomizers)
            foreach (var parameter in randomizer.parameters)
            {
                try
                {
                    parameter.Validate();
                }
                catch (ParameterValidationException exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        void OnApplicationQuit()
        {
            foreach (var randomizer in m_Randomizers)
            {
                randomizer.CleanupSamplers();
            }
        }
    }
}
