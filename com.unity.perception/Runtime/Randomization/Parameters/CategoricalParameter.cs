﻿using System;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Parameters
{
    public class CategoricalParameter<T> : TypedParameter<T>
    {
        public bool uniform;
        [Min(0)] public uint seed;
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
            for (var i = 0; i < probabilities.Length; i++)
                probabilities[i] = probabilities[i] / totalProbability;
        }

        T Sample(ref Unity.Mathematics.Random rng)
        {
            var randomValue = rng.NextFloat();
            if (uniform)
                return options[(int)(randomValue * options.Length)];

            var sum = 0f;
            for (var i = 0; i < options.Length; i++)
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