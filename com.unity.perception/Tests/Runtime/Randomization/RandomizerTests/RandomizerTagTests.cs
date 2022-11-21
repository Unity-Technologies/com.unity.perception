using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;

namespace RandomizationTests.RandomizerTests
{
    [TestFixture]
    public class RandomizerTagTests
    {
        public class BaseTag : RandomizerTag {}

        public class DerivedTag : BaseTag {}

        [TearDown]
        public void Teardown()
        {
            var tags = Object.FindObjectsOfType<BaseTag>();
            foreach (var tag in tags)
            {
                if (tag != null && tag.gameObject != null)
                    Object.DestroyImmediate(tag.gameObject);
            }
        }

        [Test]
        public void TagInheritanceWorksInTagQueries()
        {
            const int copyCount = 5;
            var gameObject = new GameObject();
            gameObject.AddComponent<BaseTag>();
            for (var i = 0; i < copyCount - 1; i++)
                Object.Instantiate(gameObject);

            var gameObject2 = new GameObject();
            gameObject2.AddComponent<DerivedTag>();
            for (var i = 0; i < copyCount - 1; i++)
                Object.Instantiate(gameObject2);

            var tagManager = RandomizerTagManager.singleton;
            var queriedBaseTags = tagManager.Query<BaseTag>().ToArray();
            Assert.AreEqual(queriedBaseTags.Length, copyCount);

            var queriedDerivedTags = tagManager.Query<DerivedTag>().ToArray();
            Assert.AreEqual(queriedDerivedTags.Length, copyCount);

            queriedBaseTags = tagManager.Query<BaseTag>(true).ToArray();
            Assert.AreEqual(queriedBaseTags.Length, copyCount * 2);
        }

        [Test]
        public void TagQueriesPreserveInsertionOrder()
        {
            const int copyCount = 5;
            const int destroyCount = 3;

            var testObj = new GameObject();
            testObj.AddComponent<BaseTag>();

            var testObjects = new List<GameObject> { testObj };

            for (var i = 0; i < copyCount - 1; i++)
                testObjects.Add(Object.Instantiate(testObj));

            for (var i = 0; i < destroyCount; i++)
            {
                Object.DestroyImmediate(testObjects[1]);
                testObjects.RemoveAt(1);
            }

            for (var i = 0; i < copyCount + destroyCount; i++)
                testObjects.Add(Object.Instantiate(testObj));

            var tagManager = RandomizerTagManager.singleton;
            var tags = tagManager.Query<BaseTag>();
            var tagsArray = tags.ToArray();

            var index = 0;
            foreach (var tag in tagsArray)
                Assert.AreEqual(tag, testObjects[index++].GetComponent<BaseTag>());
        }
    }
}
