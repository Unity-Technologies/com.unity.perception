using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine.UIElements;

namespace PerformanceTests
{
    [TestFixture(640, 480, false, false)]
    [TestFixture(640, 480, true, false)]
    [TestFixture(640, 480, true, true)]
    [TestFixture(1024, 768, false, false)]
    [TestFixture(1024, 768, true, false)]
    [TestFixture(1024, 768, true, true)]
    [TestFixture(1920, 1080, false, false)]
    [TestFixture(1920, 1080, true, false)]
    [TestFixture(1920, 1080, true, true)]
    [Category("Performance")]
    public class PerformanceTestBoundingBoxLabeler : PerformanceTester
    {
        public const string Label = "BoundingBoxLabeler";
        
        public PerformanceTestBoundingBoxLabeler(int resx, int resy, bool capData, bool vizOn)
            : base(resx, resy, capData, vizOn, Label) { }
    }
}
