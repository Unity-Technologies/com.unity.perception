using NUnit.Framework;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace RandomizationTests.SamplerTests
{
    [TestFixture]
    public class AnimationCurveSamplerTestsBase : SamplerTestsBase<AnimationCurveSampler>
    {
        public AnimationCurveSamplerTestsBase()
        {
            m_BaseSampler = new AnimationCurveSampler();
        }
    }
}
