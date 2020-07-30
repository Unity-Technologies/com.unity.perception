using System.Collections.Generic;

namespace UnityEngine.Perception.Randomization.Parameters
{
    public interface ICategoricalParameter
    {
        List<float> Probabilities { get; }
    }
}
