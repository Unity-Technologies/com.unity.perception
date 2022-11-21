namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Encapsulates the random state that all samplers mutate when generating random values
    /// </summary>
    public static class SamplerState
    {
        /// <summary>
        /// The central random state that all samplers mutate when generating random numbers
        /// </summary>
        public static uint randomState = SamplerUtility.largePrime;

        /// <summary>
        /// Creates a random number generator seeded with a unique random state
        /// </summary>
        /// <returns>The seeded random number generator</returns>
        public static Unity.Mathematics.Random CreateGenerator()
        {
            return new Unity.Mathematics.Random { state = NextRandomState() };
        }

        /// <summary>
        /// Generates a new random state and overwrites the old random state with the newly generated value
        /// </summary>
        /// <returns>The newly generated random state</returns>
        public static uint NextRandomState()
        {
            randomState = SamplerUtility.Hash32NonZero(randomState);
            return randomState;
        }
    }
}
