﻿using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers;

namespace RandomizationTests.RandomizerTests
{
    [Serializable]
    [AddRandomizerMenu("Perception Tests/Example Transform Randomizer")]
    public class ExampleTransformRandomizer : Randomizer
    {
        public Vector3Parameter position = new Vector3Parameter();
        public Vector3Parameter rotation = new Vector3Parameter();

        public Transform transform;

        protected override void OnCreate()
        {
            transform = scenario.transform;
        }

        protected override void OnUpdate()
        {
            transform.position = position.Sample();
        }

        protected override void OnIterationStart()
        {
            transform.rotation = quaternion.Euler(rotation.Sample());
        }
    }
}
