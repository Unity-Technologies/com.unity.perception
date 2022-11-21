using NUnit.Framework;
using UnityEngine.Perception.Randomization.Samplers;

namespace RandomizationTests.SamplerTests
{
    [TestFixture]
    public class SamplerUtilityTests
    {
        [Test]
        public void SeedIteratedByChangingEitherBaseSeedOrIndex()
        {
            // Check if changing the index changes the generated random state
            var iteratedIndex1 = SamplerUtility.IterateSeed(0, 0);
            var iteratedIndex2 = SamplerUtility.IterateSeed(1, 0);
            Assert.AreNotEqual(iteratedIndex1, iteratedIndex2);

            // Check if changing the seed changes the generated random state
            var iteratedSeed1 = SamplerUtility.IterateSeed(0, 0);
            var iteratedSeed2 = SamplerUtility.IterateSeed(0, 1);
            Assert.AreNotEqual(iteratedSeed1, iteratedSeed2);
        }
    }
}
