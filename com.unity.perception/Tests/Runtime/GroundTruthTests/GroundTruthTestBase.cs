using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Perception;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    public class GroundTruthTestBase
    {
        List<GameObject> objectsToDestroy = new List<GameObject>();
        [TearDown]
        public void TearDown()
        {
            foreach (var o in objectsToDestroy)
                Object.DestroyImmediate(o);

            objectsToDestroy.Clear();

            SimulationManager.ResetSimulation();
            Time.timeScale = 1;
            if (Directory.Exists(SimulationManager.OutputDirectory))
                Directory.Delete(SimulationManager.OutputDirectory, true);
        }

        public void AddTestObjectForCleanup(GameObject @object) => objectsToDestroy.Add(@object);

        public void DestroyTestObject(GameObject @object)
        {
            Object.DestroyImmediate(@object);
            objectsToDestroy.Remove(@object);
        }
    }
}
