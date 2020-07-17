using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Configuration;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    public abstract class Scenario : MonoBehaviour
    {
        [HideInInspector] public ParameterConfiguration parameterConfiguration;

        public abstract bool Running { get; }

        public virtual void Initialize() { }

        public virtual void Setup()
        {
            var iteration = parameterConfiguration.curriculum.CurrentIteration;
            foreach (var parameter in parameterConfiguration.parameters)
                parameter.Apply(iteration);
        }
        public virtual void Teardown() { }
    }
}
