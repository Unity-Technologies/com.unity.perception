using System;
using NUnit.Framework;
using UnityEngine.Perception.Randomization.Samplers;

namespace RandomizationTests.SamplerTests
{
    [TestFixture]
    public class FloatRangeTests
    {
        [Test]
        public void InvalidRange()
        {
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() => new FloatRange(1, -1).Validate());
        }
    }
}
