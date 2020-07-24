using System;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Parameters
{
    public abstract class CategoricalParameter<T> : TypedParameter<T>, ICategoricalParameter
    {
        public bool uniform;
        [Min(0)] public uint seed;
        public List<T> options = new List<T>();
        public List<float> probabilities = new List<float>();

        public List<float> Probabilities => probabilities;

        public override Sampler[] Samplers => new Sampler[0];
        public override Type OutputType => typeof(T);

        public int OptionsCount() => Math.Min(options.Count, probabilities.Count);

        public void RemoveAt(int index)
        {
            options.RemoveAt(index);
            probabilities.RemoveAt(index);
        }

        public void Resize(int size)
        {
            if (options.Count < size)
            {
                for (var i = options.Count; i < size; i++)
                    options.Add(default);
                for (var i = probabilities.Count; i < size; i++)
                    probabilities.Add(default);
            }
            else if (options.Count > size)
            {
                for (var i = options.Count - 1; i >= size; i--)
                    options.RemoveAt(i);
                for (var i = probabilities.Count - 1; i >= size; i--)
                    probabilities.RemoveAt(i);
            }
        }

        public override void Validate()
        {
            if (!uniform && probabilities.Count != options.Count)
                throw new ParameterValidationException("Number of options must be equal to the number of probabilities");

            var totalProbability = 0f;
            foreach (var probability in probabilities)
                totalProbability += probability;

            if (Mathf.Approximately(totalProbability, 0f))
                throw new ParameterValidationException("Total probability must be greater than 0");

            // Normalize probabilities
            for (var i = 0; i < probabilities.Count; i++)
                probabilities[i] = probabilities[i] / totalProbability;
        }

        T Sample(ref Unity.Mathematics.Random rng)
        {
            var randomValue = rng.NextFloat();
            if (uniform)
                return options[(int)(randomValue * options.Count)];

            var sum = 0f;
            for (var i = 0; i < options.Count; i++)
            {
                sum += probabilities[i];
                if (randomValue <= sum)
                    return options[i];
            }
            throw new ParameterException($"No option present for sampled random value {randomValue}");
        }

        public override T Sample(int iteration)
        {
            var rng = RandomUtility.RandomFromIndex((uint)iteration, seed);
            return Sample(ref rng);
        }

        public override T[] Samples(int iteration, int numSamples)
        {
            var samples = new T[numSamples];
            var rng = RandomUtility.RandomFromIndex((uint)iteration, seed);
            for (var i = 0; i < numSamples; i++)
                samples[i] = Sample(ref rng);
            return samples;
        }
    }
}
