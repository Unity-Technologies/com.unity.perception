using NUnit.Framework;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace RandomizationTests.SamplerTests
{
    [TestFixture]
    public class NormalSamplerTests : RangedSamplerTests<NormalSampler>
    {
        public NormalSamplerTests()
        {
            m_BaseSampler = new NormalSampler(-10f, 10f, 0f, 1f);
        }
    }
}
