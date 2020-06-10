using System.Collections;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Parameters.Abstractions;
using UnityEngine.Perception.Randomization.Scenarios.Abstractions;
using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios;

namespace UnityEngine.Perception.Randomization.Parameters.MonoBehaviours
{
    public class ParameterConfiguration : MonoBehaviour
    {
        public List<ParameterBase> parameters = new List<ParameterBase>();
        public Scenario scenario;

        int m_GlobalIterationIndex;
        int[] m_IterationState;
        bool m_HasScenario;

        public int GlobalIterationIndex => m_GlobalIterationIndex;

        public int TotalIterationCount
        {
            get
            {
                if (parameters.Count == 0) return 0;
                var totalIterationCount = 1;
                foreach (var param in parameters)
                {
                    totalIterationCount *= param.sampler.SampleCount;
                }
                return totalIterationCount;
            }
        }

        public int TotalFrameCount
        {
            get
            {
                var totalFrameCount = TotalIterationCount;
                if (scenario != null)
                    totalFrameCount *= scenario.FrameCount;
                return totalFrameCount;
            }
        }

        public bool FinishedIterating => m_IterationState[0] >= parameters[0].sampler.SampleCount;

        public void Awake()
        {
            if (parameters.Count == 0)
            {
                StopExecution();
                enabled = false;
                return;
            }
            m_IterationState = new int[parameters.Count];
            if (scenario == null)
                scenario = gameObject.AddComponent<EmptyScenario>();
            scenario.parameterConfiguration = this;
        }

        void Start()
        {
            StartCoroutine(UpdateLoop());
        }

        IEnumerator UpdateLoop()
        {
            yield return new WaitForSeconds(1f);
            while (!FinishedIterating)
            {
                ApplyIterationDataToParameters();
                scenario.Setup();
                for (var i = 0; i < scenario.FrameCount; i++)
                    yield return null;
                scenario.Teardown();
                Iterate();
            }
            StopExecution();
        }

        static void StopExecution()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }

        void ApplyIterationDataToParameters()
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                var currentIteration = m_IterationState[i];
                param.Apply(new IterationData {
                    localSampleIndex = currentIteration,
                    globalSampleIndex = m_GlobalIterationIndex
                });
            }
        }

        void Iterate()
        {
            var samplerIndex = m_IterationState.Length - 1;
            m_IterationState[samplerIndex]++;

            while (samplerIndex > 0 && m_IterationState[samplerIndex] == parameters[samplerIndex].sampler.SampleCount)
            {
                m_IterationState[samplerIndex--] = 0;
                m_IterationState[samplerIndex]++;
            }

            m_GlobalIterationIndex++;
        }

        public Parameter<T> GetParameter<T>(string parameterName)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.parameterName == parameterName && parameter.SampleType() == typeof(T))
                {
                    return (Parameter<T>)parameter;
                }
            }
            Debug.LogError($"Parameter {parameterName} not found");
            return null;
        }
    }
}
