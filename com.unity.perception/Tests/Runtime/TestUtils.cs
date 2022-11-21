using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Scenarios;
using Object = UnityEngine.Object;

namespace RandomizerTests.Internal
{
    public static class TestUtils
    {
        const string k_ResourcesDirectory = "Packages/com.unity.Perception.Internal/Tests/Runtime/Resource";
        const string k_ScenarioContainerName = "Scenario Container";
        const string k_TagContainerName = "Tag Container";
        const string k_CameraContainerName = "Camera Container";
        public const string cameraTag = "MainCamera";

        /// <summary>
        /// Adds a <see cref="FixedLengthScenario"/> (10 iterations, 1 frame per iteration) to the scene with
        /// a randomizer of type (T). Also adds a <see cref="RandomizerTag" /> of type (U).
        /// </summary>
        public static void SetupRandomizerTestScene<T, U>(
            ref FixedLengthScenario scenario,
            ref T randomizer,
            ref U tag,
            Action<FixedLengthScenario, T, U> initialize = null,
            bool addTag = true
        )
            where T : Randomizer
            where U : RandomizerTag
        {
            // Checks
            if (scenario != null)
            {
                scenario.enabled = false;
                Object.DestroyImmediate(scenario);
            }

            if (randomizer != null)
            {
                randomizer.enabled = false;
                randomizer = null;
            }

            if (tag != null)
            {
                Object.DestroyImmediate(tag);
                tag = null;
            }

            var mainCamera = GameObject.FindGameObjectWithTag(cameraTag);
            if (mainCamera != null)
                Object.DestroyImmediate(mainCamera);

            // adds scenario
            var scenarioContainer = new GameObject(k_ScenarioContainerName);
            scenario = scenarioContainer.AddComponent<FixedLengthScenario>();
            scenario.constants.iterationCount = 10;
            scenario.framesPerIteration = 1;
            scenario.constants.randomSeed = 10000;

            // add randomizer
            randomizer = Activator.CreateInstance<T>();
            scenario.AddRandomizer(randomizer);

            // add tag
            if (addTag)
            {
                var tagObjectPrefab = LoadTestAsset<Object>("Internal_Bucket_Multimesh");
                var tagContainer = Object.Instantiate(tagObjectPrefab) as GameObject;
                if (tagContainer == null)
                    throw new Exception("Test configured incorrectly. Tag container is null.");
                tagContainer.name = k_TagContainerName;
                tag = tagContainer.AddComponent<U>();
            }

            // add a camera
            var camera = new GameObject(k_CameraContainerName, typeof(Camera))
            {
                tag = cameraTag
            };

            // post process
            initialize?.Invoke(scenario, randomizer, tag);
        }

        /// <summary>
        /// Adds a <see cref="FixedLengthScenario"/> (10 iterations, 1 frame per iteration) to the scene with
        /// a randomizer of type (T).
        /// </summary>
        public static void SetupRandomizerTestScene<T>(
            ref FixedLengthScenario scenario,
            ref T randomizer,
            Action<FixedLengthScenario, T> initialize = null
        )
            where T : Randomizer
        {
            // to re-use the same code, we pass in a ref that will be destroyed anyway
            var tempTag = new LightRandomizerTag();
            SetupRandomizerTestScene(
                ref scenario, ref randomizer, ref tempTag,
                // wrapper for f(scenario, randomizer, tag) --> f(scenario, randomizer)
                ((x, y, z) => initialize?.Invoke(x, y)),
                false
            );
        }

        public static (T, float)[] OptionsFromArray<T>(IEnumerable<T> options) => options
        .Select(option => (option, 1f))
        .ToArray();

        public static T LoadTestAsset<T>(string name) where T : Object
        {
            return Resources.Load<T>(name);
        }

        public static void ResetScene()
        {
            var scenarioContainer = GameObject.Find(k_ScenarioContainerName);
            var tagContainer = GameObject.Find(k_TagContainerName);
            var cameraContainer = GameObject.Find(k_CameraContainerName);

            Object.DestroyImmediate(scenarioContainer);
            Object.DestroyImmediate(tagContainer);
            Object.DestroyImmediate(cameraContainer);
        }
    }
}
