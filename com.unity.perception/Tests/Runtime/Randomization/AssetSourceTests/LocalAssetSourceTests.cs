using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization;

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

            public override void Preprocess(GameObject asset)
            {
                throw new System.NotImplementedException();
            }
        }

        class TestBehaviour : MonoBehaviour
        {
            public AssetSource<GameObject> gameObjectSource = new AssetSource<GameObject>
            {
                assetRole = null,
                assetSourceLocation = new LocalAssetSourceLocation()
            };

            public AssetSource<Material> materialSource = new AssetSource<Material>
            {
                assetRole = null,
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
    }
}
