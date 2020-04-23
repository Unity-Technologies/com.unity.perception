using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    public class GroundTruthTestBase
    {
        List<GameObject> m_ObjectsToDestroy = new List<GameObject>();
        [TearDown]
        public void TearDown()
        {
            foreach (var o in m_ObjectsToDestroy)
                Object.DestroyImmediate(o);

            m_ObjectsToDestroy.Clear();

            SimulationManager.ResetSimulation();
            Time.timeScale = 1;
            if (Directory.Exists(SimulationManager.OutputDirectory))
                Directory.Delete(SimulationManager.OutputDirectory, true);
        }

        public void AddTestObjectForCleanup(GameObject @object) => m_ObjectsToDestroy.Add(@object);

        public void DestroyTestObject(GameObject @object)
        {
            Object.DestroyImmediate(@object);
            m_ObjectsToDestroy.Remove(@object);
        }
    }
}
