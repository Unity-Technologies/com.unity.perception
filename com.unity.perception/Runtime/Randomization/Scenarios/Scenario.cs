using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    public abstract class Scenario : MonoBehaviour
    {
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
        public virtual int currentIteration { get; protected set; }

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
        /// Can be overriden to serialize scenario information to the JSON parameter configuration file
        /// </summary>
        public virtual JObject Serialize() { return null; }

        /// <summary>
        /// Can be overriden to deserialized scenario information to the JSON parameter configuration file
        /// </summary>
        public virtual void Deserialize(JObject token) { }
    }
}
