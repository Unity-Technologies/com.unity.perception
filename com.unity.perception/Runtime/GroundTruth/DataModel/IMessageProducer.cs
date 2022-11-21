namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// Interface for classes that can write their contents to a <see cref="IMessageBuilder" />
    /// </summary>
    public interface IMessageProducer
    {
        /// <summary>
        /// Convert contents int a message.
        /// </summary>
        /// <param name="builder">The message builder that will convert the class's contents into a message</param>
        void ToMessage(IMessageBuilder builder);
    }
}
