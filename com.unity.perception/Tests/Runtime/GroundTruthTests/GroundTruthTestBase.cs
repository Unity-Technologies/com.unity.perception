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
        List<Object> m_ObjectsToDestroy = new List<Object>();
        [TearDown]
        public void TearDown()
        {
            foreach (var o in m_ObjectsToDestroy)
                Object.DestroyImmediate(o);

            m_ObjectsToDestroy.Clear();

            DatasetCapture.ResetSimulation();
            Time.timeScale = 1;
            if (Directory.Exists(DatasetCapture.OutputDirectory))
                Directory.Delete(DatasetCapture.OutputDirectory, true);
        }

        public void AddTestObjectForCleanup(Object @object) => m_ObjectsToDestroy.Add(@object);

        public void DestroyTestObject(Object @object)
        {
            Object.DestroyImmediate(@object);
            m_ObjectsToDestroy.Remove(@object);
        }
    }
}
