using NUnit.Framework;
using NativeControls.Deiver;

namespace DriverGenerateTest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            new IDD_ABOUTBOXDriver(null);
            Assert.Pass();
        }
    }
}