using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    public class PassTestBase
    {
        List<GameObject> objectsToDestroy = new List<GameObject>();
        [TearDown]
        public void TearDown()
        {
            foreach (var o in objectsToDestroy)
                Object.DestroyImmediate(o);

            objectsToDestroy.Clear();
            SimulationManager.ResetSimulation();
        }

        public void AddTestObjectForCleanup(GameObject @object) => objectsToDestroy.Add(@object);

        public void DestroyTestObject(GameObject @object)
        {
            Object.DestroyImmediate(@object);
            objectsToDestroy.Remove(@object);
        }
    }
}
