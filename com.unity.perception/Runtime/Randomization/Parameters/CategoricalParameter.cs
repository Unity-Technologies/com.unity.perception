using System;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Parameters
{
    public class CategoricalParameter<T> : Parameter
    {
        public bool uniform;
        public uint baseRandomSeed;
        public T[] options;
        public float[] probabilities;
        public override Sampler[] Samplers => new Sampler[0];
        public override Type OutputType => typeof(T);

        public override void Validate()
        {
            if (!uniform && probabilities.Length != options.Length)
                throw new ParameterValidationException("Number of options must be equal to the number of probabilities");

            var totalProbability = 0f;
            foreach (var probability in probabilities)
                totalProbability += probability;

            if (Mathf.Approximately(totalProbability, 0f))
                throw new ParameterValidationException("Total probability must be greater than 0");

            // Normalize probabilities
            float probabilitySum = 0f;
            for (var i = 0; i < probabilities.Length; i++)
            {
                probabilitySum += probabilities[i] / totalProbability;
                probabilities[i] = probabilitySum;
            }
        }

        public T Sample(int iteration)
        {
            var rng = RandomUtility.RandomFromIndex((uint)iteration, baseRandomSeed);
            var randomValue = rng.NextFloat();
            if (uniform)
                return options[(int)(randomValue * options.Length)];

            for (var i = 0; i < options.Length; i++)
            {
                if (randomValue <= probabilities[i])
                    return options[i];
            }
            throw new ParameterException($"No option present for sampled random value {randomValue}");
        }
    }
}
