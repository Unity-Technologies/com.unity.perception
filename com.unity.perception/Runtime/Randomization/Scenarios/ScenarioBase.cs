using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Perception.Randomization.Configuration;
using UnityEngine.Perception.Randomization.Parameters;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    public abstract class ScenarioBase : MonoBehaviour
    {
        static ScenarioBase s_ActiveScenario;
        [HideInInspector] public bool quitOnComplete = true;
        [HideInInspector] public bool deserializeOnStart;
        [HideInInspector] public string serializedConstantsFileName = "constants";

        /// <summary>
        /// Returns the active parameter scenario in the scene
        /// </summary>
        public static ScenarioBase ActiveScenario
        {
            get => s_ActiveScenario;
            private set
            {
                if (s_ActiveScenario != null)
                    throw new ScenarioException("There cannot be more than one active ParameterConfiguration");
                s_ActiveScenario = value;
            }
        }

        /// <summary>
        /// Returns the file location of the JSON serialized constants
        /// </summary>
        public string serializedConstantsFilePath =>
            Application.dataPath + "/StreamingAssets/" + serializedConstantsFileName + ".json";

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
        /// Called before the scenario begins iterating
        /// </summary>
        public virtual void OnInitialize() { }

        /// <summary>
        /// Called when each scenario iteration starts
        /// </summary>
        public virtual void OnIterationSetup() { }

        /// <summary>
        /// Called at the start of every frame
        /// </summary>
        public virtual void OnFrameStart() { }

        /// <summary>
        /// Called right before the scenario iterates
        /// </summary>
        public virtual void OnIterationTeardown() { }

        /// <summary>
        /// Called when the scenario has finished iterating
        /// </summary>
        public virtual void OnComplete() { }

        /// <summary>
        /// To be overriden in derived Scenario classes
        /// </summary>
        public abstract string OnSerialize();

        /// <summary>
        /// To be overriden in derived Scenario classes
        /// </summary>
        public abstract void OnDeserialize(string json);

        /// <summary>
        /// Serializes the scenario's constants to a JSON file located at serializedConstantsFilePath
        /// </summary>
        public abstract void Serialize();

        /// <summary>
        /// Deserializes constants saved in a JSON file located at serializedConstantsFilePath
        /// </summary>
        public abstract void Deserialize();


        void OnEnable()
        {
            ActiveScenario = this;
            if (deserializeOnStart)
                Deserialize();
        }

        void OnDisable()
        {
            s_ActiveScenario = null;
        }

        void Start()
        {
            foreach (var config in ParameterConfiguration.configurations)
                config.ValidateParameters();
            OnInitialize();
            StartCoroutine(UpdateLoop());
        }

        IEnumerator UpdateLoop()
        {
            while (!isScenarioComplete)
            {
                foreach (var config in ParameterConfiguration.configurations)
                    config.ApplyParameters(currentIteration, ParameterApplicationFrequency.OnIterationSetup);
                OnIterationSetup();
                while (!isIterationComplete)
                {
                    foreach (var config in ParameterConfiguration.configurations)
                        config.ApplyParameters(framesSinceInitialization, ParameterApplicationFrequency.EveryFrame);
                    OnFrameStart();
                    yield return null;
                    currentIterationFrame++;
                    framesSinceInitialization++;
                }
                OnIterationTeardown();
                currentIteration++;
                currentIterationFrame = 0;
            }
            OnComplete();
            QuitApplication();
        }

        void QuitApplication()
        {
            if (quitOnComplete)
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }
    }
}
