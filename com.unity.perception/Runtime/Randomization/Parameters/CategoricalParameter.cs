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
        [SerializeField] internal bool uniform;
        [SerializeField] UniformSampler m_Sampler = new UniformSampler(0f, 1f);

        [SerializeField] List<T> m_Options = new List<T>();
        float[] m_NormalizedProbabilities;

        public override ISampler[] samplers => new ISampler[] { m_Sampler };

        public sealed override Type OutputType => typeof(T);

        public T GetOption(int index) => m_Options[index];

        public float GetProbability(int index) => probabilities[index];

        internal CategoricalParameter() { }

        /// <summary>
        /// Create a new categorical parameter from a list of categories with uniform probabilities
        /// </summary>
        /// <param name="categoricalOptions">List of categorical options</param>
        public CategoricalParameter(IEnumerable<T> categoricalOptions)
        {
            if (options.Count == 0)
                throw new ArgumentException("List of options is empty");
            uniform = true;
            foreach (var option in categoricalOptions)
                AddOption(option, 1f);
        }

        /// <summary>
        /// Create a new categorical parameter from a list of categories and their associated probabilities
        /// </summary>
        /// <param name="categoricalOptions">List of options and their associated probabilities</param>
        public CategoricalParameter(IEnumerable<(T, float)> categoricalOptions)
        {
            if (options.Count == 0)
                throw new ArgumentException("List of options is empty");
            foreach (var (category, probability) in categoricalOptions)
                AddOption(category, probability);
            NormalizeProbabilities();
        }

        internal override void AddOption()
        {
            m_Options.Add(default);
            probabilities.Add(0f);
        }

        internal void AddOption(T option, float probability)
        {
            m_Options.Add(option);
            probabilities.Add(probability);
        }

        internal override void RemoveOption(int index)
        {
            m_Options.RemoveAt(index);
            probabilities.RemoveAt(index);
        }

        internal override void ClearOptions()
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
            }
        }

        internal void NormalizeProbabilities()
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

        /// <summary>
        /// Generates one parameter sample
        /// </summary>
        public T Sample()
        {
            var randomValue = m_Sampler.Sample();
            return uniform
                ? m_Options[(int)(randomValue * m_Options.Count)]
                : m_Options[BinarySearch(randomValue)];
        }

        public sealed override void ApplyToTarget(int seedOffset)
        {
            if (!hasTarget)
                return;
            target.ApplyValueToTarget(Sample());
        }
    }
}
