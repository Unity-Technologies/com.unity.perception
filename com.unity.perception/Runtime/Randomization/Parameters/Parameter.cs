using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    public abstract class Parameter : MonoBehaviour
    {
        public string parameterName = "Parameter Name";

        public abstract Sampler[] Samplers { get; }

        public abstract Type OutputType { get; }

        // public bool hasTarget = true;
        // public PropertyTarget target;

        // public void Apply()
        // {
        //     if (!hasTarget)
        //         return;
        //     space.Apply(target, samplerBase.Sample());
        // }

        public void OnDestroy()
        {
            foreach (var sampler in Samplers)
                DestroyImmediate(sampler);
        }
    }

    [Serializable]
    public class PropertyTarget
    {
        public GameObject gameObject;
        public Component component;
        public string propertyName;
        public FieldOrProperty fieldOrProperty;
    }

    public enum FieldOrProperty
    {
        Field, Property
    }
}
