using System;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// The base class for implementing custom scenario constants classes
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Experimental.Perception.Randomization.Scenarios")]
    public class ScenarioConstants
    {
        /// <summary>
        /// The starting value initializing all random value sequences generated through Samplers, Parameters, and
        /// Randomizers attached to a Scenario
        /// </summary>
        [Tooltip("The starting value initializing all random value sequences generated through Samplers, Parameters, and Randomizers attached to a Scenario")]
        public uint randomSeed = SamplerUtility.largePrime;
    }
}
