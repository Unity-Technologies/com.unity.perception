using System;

namespace UnityEngine.Perception.GroundTruth.Consumers
{
    /// <summary>
    /// This is attribute marks an EndpointConsumer to not be visible to the user in the endpoint create menu. Some
    /// endpoints are designed to be used internally and should not be made public to the user.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class HideFromCreateMenuAttribute : Attribute {}
}
