using NUnit.Framework;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace RandomizationTests.SamplerTests
{
    [TestFixture]
    public class AnimationCurveSamplerTests : RangedSamplerTests<AnimationCurveSampler>
    {
        public AnimationCurveSamplerTests()
        {
            m_BaseSampler = new AnimationCurveSampler();
            m_BaseSampler.range = new FloatRange(m_BaseSampler.distributionCurve.keys[0].time, m_BaseSampler.distributionCurve.keys[m_BaseSampler.distributionCurve.length - 1].time);
        }
    }
}
