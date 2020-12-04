using NUnit.Framework;

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
    public class PerformanceTestObjectCountLabeler : PerformanceTester
    {
        public const string Label = "ObjectCountLabeler";
        
        public PerformanceTestObjectCountLabeler(int resx, int resy, bool capData, bool vizOn)
            : base(resx, resy, capData, vizOn, Label)
        {
        }
    }
}
