using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Perception.Randomization.Configuration;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    public abstract class ScenarioBase : MonoBehaviour
    {
        static ScenarioBase s_ActiveScenario;
        public bool quitOnComplete = true;
        [HideInInspector] public bool deserializeOnStart;
        [HideInInspector] public string serializedConstantsFileName = "constants";

        /// <summary>
        /// Returns the file location of the JSON serialized constants
        /// </summary>
        public string serializedConstantsFilePath =>
            Application.dataPath + "/StreamingAssets/" + serializedConstantsFileName + ".json";

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
        /// The number of frames that have elapsed over the current iteration
        /// </summary>
        public int iterationFrameCount { get; private set; }

        /// <summary>
        /// Returns whether the current scenario iteration has completed
        /// </summary>
        public abstract bool isIterationComplete { get; }

        /// <summary>
        /// Returns whether the entire scenario has completed
        /// </summary>
        public abstract bool isScenarioComplete { get; }

        /// <summary>
        /// The current iteration index of the scenario
        /// </summary>
        public int currentIteration { get; protected set; }

        internal void NextFrame()
        {
            iterationFrameCount++;
        }

        internal void Iterate()
        {
            currentIteration++;
            iterationFrameCount = 0;
        }

        /// <summary>
        /// Called before the scenario begins iterating
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called when each scenario iteration starts
        /// </summary>
        public virtual void Setup() { }

        /// <summary>
        /// Called right before the scenario iterates
        /// </summary>
        public virtual void Teardown() { }

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
        /// To be overriden in the Scenario class
        /// </summary>
        public abstract void Serialize();

        /// <summary>
        /// To be overriden in the Scenario class
        /// </summary>
        public abstract void Deserialize();

        #region Monobehaviour Methods
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
            Initialize();
            StartCoroutine(UpdateLoop());
        }

        IEnumerator UpdateLoop()
        {
            while (!isScenarioComplete)
            {
                foreach (var config in ParameterConfiguration.configurations)
                    config.ApplyParameters(currentIteration);
                Setup();

                while (!isIterationComplete)
                {
                    yield return null;
                    NextFrame();
                }

                Teardown();
                Iterate();
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
        #endregion
    }
}
