using System;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    public abstract class ScenarioBase : MonoBehaviour
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

        public abstract string Serialize();
        public abstract void Deserialize(string json);
    }
}
