using System;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// Generates samples by choosing one option from a weighted list of choices.
    /// </summary>
    [Serializable]
    public abstract class CategoricalParameter<T> : CategoricalParameterBase
    {
        public bool uniform;
        public uint seed;
        [SerializeField]
        List<T> m_Options = new List<T>();

        float[] m_NormalizedProbabilities;

        public override ISampler[] Samplers => new ISampler[0];
        public sealed override Type OutputType => typeof(T);
        public T GetOption(int index) => m_Options[index];
        public float GetProbability(int index) => probabilities[index];

        internal override void AddOption()
        {
            m_Options.Add(default);
            probabilities.Add(0f);
        }

        public void AddOption(T option, float probability)
        {
            m_Options.Add(option);
            probabilities.Add(probability);
        }

        public override void RemoveOption(int index)
        {
            m_Options.RemoveAt(index);
            probabilities.RemoveAt(index);
        }

        public override void ClearOptions()
        {
            m_Options.Clear();
            probabilities.Clear();
        }

        public IReadOnlyList<(T, float)> options
        {
            get
            {
                var catOptions = new List<(T, float)>(m_Options.Count);
                for (var i = 0; i < catOptions.Count; i++)
                    catOptions.Add((m_Options[i], probabilities[i]));
                return catOptions;
            }
        }

        public override void Validate()
        {
            base.Validate();
            if (!uniform)
            {
                if (probabilities.Count != m_Options.Count)
                    throw new ParameterValidationException(
                        "Number of options must be equal to the number of probabilities");
                NormalizeProbabilities();
            }
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

        T Sample(ref Unity.Mathematics.Random rng)
        {
            var randomValue = rng.NextFloat();
            return uniform
                ? m_Options[(int)(randomValue * m_Options.Count)]
                : m_Options[BinarySearch(randomValue)];
        }

        /// <summary>
        /// Generates one parameter sample
        /// </summary>
        /// <param name="index">Often the current scenario iteration or a scenario's framesSinceInitialization</param>
        public T Sample(int index)
        {
            NormalizeProbabilities();
            var iteratedSeed = SamplerUtility.IterateSeed((uint)index, seed);
            var rng = new Unity.Mathematics.Random(iteratedSeed);
            return Sample(ref rng);
        }

        /// <summary>
        /// Generates an array of parameter samples
        /// </summary>
        /// <param name="index">Often the current scenario iteration or a scenario's framesSinceInitialization</param>
        /// <param name="sampleCount">Number of parameter samples to generate</param>
        public T[] Samples(int index, int sampleCount)
        {
            NormalizeProbabilities();
            var samples = new T[sampleCount];
            var iteratedSeed = SamplerUtility.IterateSeed((uint)index, seed);
            var rng = new Unity.Mathematics.Random(iteratedSeed);
            for (var i = 0; i < sampleCount; i++)
                samples[i] = Sample(ref rng);
            return samples;
        }

        public sealed override void ApplyToTarget(int seedOffset)
        {
            if (!hasTarget)
                return;
            target.ApplyValueToTarget(Sample(seedOffset));
        }
    }
}
