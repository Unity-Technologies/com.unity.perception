using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Perception.Content;

namespace CharacterToolingTests
{
    public class CharacterToolingTests
    {
        public List<GameObject> selectionLists = new List<GameObject>();
        public List<string> assets = new List<string>();
        private CharacterTooling contentTests = new CharacterTooling();

        [SetUp]
        public void Setup()
        {
            selectionLists = new List<GameObject>();
            AssetListCreation();
        }

        [TearDown]
        public void TearDown()
        {
            selectionLists.Clear();
        }

        [Test, TestCaseSource(typeof(AssetCollection), "GameObject")]
        public void CreateEarsNoseJoints(GameObject gameObject)
        {
            var model = contentTests.CharacterCreateNose(gameObject, true);
            var validate = false;
            if (model)
                validate = contentTests.ValidateNoseAndEars(gameObject);
            else if (!model)
                Assert.Fail("Failed to create the Ear and Nose Joints");

            Assert.True(validate, "Failed to create ear and nose joints");
        }

        [Test, TestCaseSource(typeof(AssetCollection), "GameObject")]
        public void CharacterBones(GameObject gameObject)
        {
            var failedBones = new Dictionary<HumanBone, bool>();
            var test = contentTests.CharacterRequiredBones(gameObject, out failedBones);

            Assert.True(test, "Character is missing required bones");
        }

        [Test, TestCaseSource(typeof(AssetCollection), "GameObject")]
        public void CharacterPoseData(GameObject gameObject)
        {
            var failedPose = new List<GameObject>();
            var test = contentTests.CharacterPoseData(gameObject, out failedPose);

            Assert.True(test, "Character is missing Pose Data");
        }

        public void AssetListCreation()
        {
            assets = AssetDatabase.GetAllAssetPaths().Where(o => o.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (string o in assets)
            {
                if (o.Contains("TestAssets/Characters"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(o);

                    if (asset != null && !selectionLists.Contains(asset))
                        selectionLists.Add(asset);
                }
            }
        }

    }

    public class AssetCollection
    {
        public static IEnumerable<TestCaseData> GameObject
        {
            get
            {
                CharacterToolingTests tool = new CharacterToolingTests();
                tool.AssetListCreation();
                foreach (var asset in tool.selectionLists)
                {
                    yield return new TestCaseData(asset);
                }
            }
        }
    }
}
