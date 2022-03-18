using System;
using UnityEngine;

namespace UnityEditor.Perception.GroundTruth
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConsumerEndpointDrawerAttribute : PropertyAttribute
    {
        internal Type consumerEndpointType;

        public ConsumerEndpointDrawerAttribute(Type consumerEndpointType)
        {
            this.consumerEndpointType = consumerEndpointType;
        }
    }
}
