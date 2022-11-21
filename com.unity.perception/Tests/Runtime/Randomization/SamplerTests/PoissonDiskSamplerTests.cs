using NUnit.Framework;
using UnityEngine.Perception.Randomization.Utilities;

namespace RandomizationTests.SamplerTests
{
    [TestFixture]
    public class PoissonDiskSamplerTests
    {
        [Test]
        public void GenerateSamplesCreatesMoreThanOnePoint()
        {
            var poissonPoints = PoissonDiskSampling.GenerateSamples(10f, 10f, 2f);
            var numPoints = poissonPoints.Length;
            poissonPoints.Dispose();
            Assert.Greater(numPoints, 1);
        }
    }
}
