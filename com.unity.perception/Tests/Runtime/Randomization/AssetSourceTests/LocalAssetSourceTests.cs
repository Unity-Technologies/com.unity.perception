using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization;
using UnityEngine.Perception.Randomization.Randomizers.Tags;

namespace RandomizationTests.AssetSourceTests
{
    [TestFixture]
    public class LocalAssetSourceTests
    {
        GameObject m_TestObject;
        TestBehaviour m_Behaviour;

        class TestAssetRole : AssetRole<GameObject>
        {
            public override string label => "test";
            public override string description => "";

            public override void Preprocess(GameObject asset)
            {
                asset.AddComponent<RotationRandomizerTag>();
            }
        }

        class TestBehaviour : MonoBehaviour
        {
            public AssetSource<GameObject> gameObjectSource = new AssetSource<GameObject>
            {
                assetRole = new TestAssetRole(),
                assetSourceLocation = new LocalAssetSourceLocation()
            };
        }

        [SetUp]
        public void Setup()
        {
            m_TestObject = new GameObject();
            m_Behaviour = m_TestObject.AddComponent<TestBehaviour>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
        }

        [Test]
        public void GetZeroCountWithoutThrowingException()
        {
            Assert.DoesNotThrow(() =>
            {
                var count = m_Behaviour.gameObjectSource.count;
            });
        }

        [Test]
        public void SampleFromEmptySourceReturnsNull()
        {
            Assert.IsNull(m_Behaviour.gameObjectSource.SampleAsset());
            Assert.IsNull(m_Behaviour.gameObjectSource.SampleInstance());
        }
    }
}
