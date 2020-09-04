using NUnit.Framework;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace RandomizationTests.SamplerTests
{
    [TestFixture]
    public class UniformSamplerTests : RangedSamplerTests<UniformSampler>
    {
        public UniformSamplerTests()
        {
            m_BaseSampler = new UniformSampler(0f, 1f);
        }
    }
}
