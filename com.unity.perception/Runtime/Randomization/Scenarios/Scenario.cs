using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Perception.Randomization.Configuration;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    public abstract class Scenario : MonoBehaviour
    {
        [HideInInspector] public ParameterConfiguration parameterConfiguration;

        public abstract bool Running { get; }

        public abstract bool Complete { get; }

        public abstract int CurrentIteration { get; }

        public abstract void Iterate();

        public virtual void Initialize() { }

        public virtual void Setup() { }

        public virtual void Teardown() { }

        public virtual JObject Serialize()
        {
            return null;
        }

        public virtual void Deserialize(JObject token) { }
    }
}
