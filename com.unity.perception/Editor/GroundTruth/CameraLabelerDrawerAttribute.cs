using System;
using UnityEngine.Perception.GroundTruth;

namespace UnityEditor.Perception.GroundTruth
{
    /// <summary>
    /// Attribute for <see cref="CameraLabelerDrawer"/> types which specifies the <see cref="CameraLabeler"/> type
    /// whose inspector should be drawn using the decorated type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CameraLabelerDrawerAttribute : Attribute
    {
        internal Type targetLabelerType;

        /// <summary>
        /// Creates a new CameraLabelerDrawerAttribute specifying the <see cref="CameraLabeler"/> type to be drawn
        /// </summary>
        /// <param name="targetLabelerType">The type whose inspector should be drawn by the decorated type</param>
        public CameraLabelerDrawerAttribute(Type targetLabelerType)
        {
            this.targetLabelerType = targetLabelerType;
        }
    }
}
