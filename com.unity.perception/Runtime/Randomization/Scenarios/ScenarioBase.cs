using System;
using System.Collections.Generic;
using Unity.Simulation;
using UnityEngine;
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

        bool m_SkipFrame = true;
        bool m_FirstScenarioFrame = true;
        bool m_WaitingForFinalUploads;
        RandomizerTagManager m_TagManager = new RandomizerTagManager();

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
        /// The RandomizerTagManager attached to this scenario
        /// </summary>
        public RandomizerTagManager tagManager => m_TagManager;

        /// <summary>
        /// Return the list of randomizers attached to this scenario
        /// </summary>
        public IReadOnlyList<Randomizer> randomizers => m_Randomizers.AsReadOnly();

        /// <summary>
        /// If true, this scenario will quit the Unity application when it's finished executing
        /// </summary>
        [HideInInspector] public bool quitOnComplete = true;

        /// <summary>
        /// The name of the Json file this scenario's constants are serialized to/from.
        /// </summary>
        [HideInInspector] public string serializedConstantsFileName = "constants";

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
        /// Returns the file location of the JSON serialized constants
        /// </summary>
        public string serializedConstantsFilePath =>
            Application.dataPath + "/StreamingAssets/" + serializedConstantsFileName + ".json";

        /// <summary>
        /// Returns this scenario's non-typed serialized constants
        /// </summary>
        public abstract object genericConstants { get; }

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
        /// Serializes the scenario's constants to a JSON file located at serializedConstantsFilePath
        /// </summary>
        public abstract void Serialize();

        /// <summary>
        /// Deserializes constants saved in a JSON file located at serializedConstantsFilePath
        /// </summary>
        public abstract void Deserialize();

        /// <summary>
        /// This method executed directly after this scenario has been registered and initialized
        /// </summary>
        protected virtual void OnAwake() { }

        void Awake()
        {
            activeScenario = this;
            OnAwake();
            foreach (var randomizer in m_Randomizers)
                randomizer.Initialize(this, tagManager);
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
            s_ActiveScenario = null;
        }

        void Start()
        {
            Deserialize();
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
                IterateParameterStates();
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
            newRandomizer.Initialize(this, tagManager);
            newRandomizer.Create();
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
        /// Generates a random seed by hashing the current scenario iteration with a given base random seed
        /// </summary>
        /// <param name="baseSeed">Used to offset the seed generator</param>
        /// <returns>The generated random seed</returns>
        public uint GenerateRandomSeed(uint baseSeed = SamplerUtility.largePrime)
        {
            var seed = SamplerUtility.IterateSeed((uint)currentIteration, baseSeed);
            return SamplerUtility.IterateSeed((uint)currentIteration, seed);
        }

        /// <summary>
        /// Generates a random seed by hashing three values together: an arbitrary index value,
        /// the current scenario iteration, and a base random seed. This method is useful for deterministically
        /// generating random seeds from within a for-loop.
        /// </summary>
        /// <param name="iteration">An offset value hashed inside the seed generator</param>
        /// <param name="baseSeed">An offset value hashed inside the seed generator</param>
        /// <returns>The generated random seed</returns>
        public uint GenerateRandomSeedFromIndex(int iteration, uint baseSeed = SamplerUtility.largePrime)
        {
            var seed =  SamplerUtility.IterateSeed((uint)iteration, baseSeed);
            return SamplerUtility.IterateSeed((uint)currentIteration, seed);
        }

        void ValidateParameters()
        {
            foreach (var randomizer in m_Randomizers)
            foreach (var parameter in randomizer.parameters)
                parameter.Validate();
        }

        void IterateParameterStates()
        {
            foreach (var randomizer in m_Randomizers)
            {
                foreach (var parameter in randomizer.parameters)
                {
                    parameter.ResetState();
                    parameter.IterateState(currentIteration);
                }
            }
        }
    }
}
