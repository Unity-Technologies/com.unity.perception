using System.Collections.Generic;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// Exposes the probabilities property of categorical parameters for UI purposes
    /// </summary>
    public interface ICategoricalParameter
    {
        List<float> Probabilities { get; }
    }
}
