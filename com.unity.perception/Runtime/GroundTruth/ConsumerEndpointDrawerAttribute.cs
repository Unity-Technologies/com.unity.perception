using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Attribute for consumers endpoints
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [MovedFrom("UnityEditor.Perception.GroundTruth")]
    public class ConsumerEndpointDrawerAttribute : PropertyAttribute
    {
        internal Type consumerEndpointType;

        /// <summary>
        /// Public constructor for ConsumerEndpointDrawerAttribute
        /// </summary>
        /// <param name="consumerEndpointType">Type to be used</param>
        public ConsumerEndpointDrawerAttribute(Type consumerEndpointType)
        {
            this.consumerEndpointType = consumerEndpointType;
        }
    }
}
