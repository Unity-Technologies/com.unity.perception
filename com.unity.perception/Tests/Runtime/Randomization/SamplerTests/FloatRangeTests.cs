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
            Assert.Throws<ArgumentException>(() => new FloatRange(1, -1).Validate());
        }
    }
}
