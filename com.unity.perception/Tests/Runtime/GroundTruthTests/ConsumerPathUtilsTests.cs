using System.IO;
using NUnit.Framework;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class ConsumerPathUtilsTests
    {
        [Test]
        public void TestDatasetNames()
        {
            var nameOk = true;

            var datasetName = "dataset";
            nameOk = PathUtils.DoesFilenameIncludeIllegalCharacters(datasetName);
            Assert.IsFalse(nameOk);
            (nameOk, _) = PathUtils.CheckAndFixFileName(datasetName);
            Assert.IsTrue(nameOk);

            LogAssert.ignoreFailingMessages = true;

            var chars = Path.GetInvalidFileNameChars();
            foreach (var c in chars)
            {
                datasetName = c + "dataset";
                nameOk = PathUtils.DoesFilenameIncludeIllegalCharacters(datasetName);
                Assert.IsTrue(nameOk);
                (nameOk, datasetName) = PathUtils.CheckAndFixFileName(datasetName);
                Assert.IsFalse(nameOk);
                Assert.AreEqual("_dataset", datasetName);
            }

            foreach (var c in chars)
            {
                datasetName = "dataset" + c;
                nameOk = PathUtils.DoesFilenameIncludeIllegalCharacters(datasetName);
                Assert.IsTrue(nameOk);
                (nameOk, datasetName) = PathUtils.CheckAndFixFileName(datasetName);
                Assert.IsFalse(nameOk);
                Assert.AreEqual("dataset_", datasetName);
            }

            foreach (var c in chars)
            {
                datasetName = "dataset" + c + "dataset";
                nameOk = PathUtils.DoesFilenameIncludeIllegalCharacters(datasetName);
                Assert.IsTrue(nameOk);
                (nameOk, datasetName) = PathUtils.CheckAndFixFileName(datasetName);
                Assert.IsFalse(nameOk);
                Assert.AreEqual("dataset_dataset", datasetName);
            }

            LogAssert.ignoreFailingMessages = false;
        }
    }
}
