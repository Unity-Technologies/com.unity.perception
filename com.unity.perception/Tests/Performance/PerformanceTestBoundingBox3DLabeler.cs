#if PERFORMANCE_TESTING_PRESENT
using NUnit.Framework;

namespace PerformanceTests
{
    [TestFixture(1920, 1080, false, false)]
    [TestFixture(1920, 1080, true, false)]
    [TestFixture(1920, 1080, true, true)]
    [Category("Performance")]
    public class PerformanceTestBoundingBox3DLabeler : PerformanceTester
    {
        public const string Label = "BoundingBox3DLabeler";

        public PerformanceTestBoundingBox3DLabeler(int resx, int resy, bool capData, bool vizOn)
            : base(resx, resy, capData, vizOn, Label) {}
    }
}
#endif
