using System;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Scenarios
{
    [Serializable]
    public class ScenarioConstants
    {
        public uint randomSeed = SamplerUtility.largePrime;
    }
}
