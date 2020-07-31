using System;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// Generates samples by choosing one option from a weighted list of choices.
    /// </summary>
    public abstract class CategoricalParameter<T> : TypedParameter<T>, ICategoricalParameter
    {
        public bool uniform;
        [Min(0)] public uint seed;
        public List<T> options = new List<T>();
        public List<float> probabilities = new List<float>();

        float[] m_NormalizedProbabilities;

        public List<float> Probabilities => probabilities;
        public override Sampler[] Samplers => new Sampler[0];

        public override void Validate()
        {
            base.Validate();

            if (uniform)
                return;

            if (probabilities.Count != options.Count)
                throw new ParameterValidationException("Number of options must be equal to the number of probabilities");

            NormalizeProbabilities();
        }

        void NormalizeProbabilities()
        {
            var totalProbability = 0f;
            for (var i = 0; i < probabilities.Count; i++)
            {
                var probability = probabilities[i];
                if (probability < 0f)
                    throw new ParameterValidationException($"Found negative probability at index {i}");
                totalProbability += probability;
            }

            if (totalProbability <= 0f)
                throw new ParameterValidationException("Total probability must be greater than 0");

            var sum = 0f;
            m_NormalizedProbabilities = new float[probabilities.Count];
            for (var i = 0; i < probabilities.Count; i++)
            {
                sum += probabilities[i] / totalProbability;
                m_NormalizedProbabilities[i] = sum;
            }
        }

        T Sample(ref Unity.Mathematics.Random rng)
        {
            var randomValue = rng.NextFloat();
            return uniform
                ? options[(int)(randomValue * options.Count)]
                : options[BinarySearch(randomValue)];
        }

        public override T Sample(int iteration)
        {
            NormalizeProbabilities();
            var rng = RandomUtility.RandomFromIndex((uint)iteration, seed);
            return Sample(ref rng);
        }

        public override T[] Samples(int iteration, int totalSamples)
        {
            NormalizeProbabilities();
            var samples = new T[totalSamples];
            var rng = RandomUtility.RandomFromIndex((uint)iteration, seed);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = Sample(ref rng);
            return samples;
        }

        int BinarySearch(float key) {
            var minNum = 0;
            var maxNum = m_NormalizedProbabilities.Length - 1;

            while (minNum <= maxNum) {
                var mid = (minNum + maxNum) / 2;
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (key == m_NormalizedProbabilities[mid]) {
                    return ++mid;
                }
                if (key < m_NormalizedProbabilities[mid]) {
                    maxNum = mid - 1;
                }
                else {
                    minNum = mid + 1;
                }
            }
            return minNum;
        }
    }
}
