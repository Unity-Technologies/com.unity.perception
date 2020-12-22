using System;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Scenarios
{
    /// <summary>
    /// The base class for implementing custom scenario constants classes
    /// </summary>
    [Serializable]
    public class ScenarioConstants
    {
        /// <summary>
        /// The starting value initializing all random values sequences generated through Samplers, Parameters, and
        /// Randomizers attached to a Scenario
        /// </summary>
        public uint randomSeed = SamplerUtility.largePrime;
    }
}
