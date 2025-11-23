namespace FlickerFlow.Abstractions;

/// <summary>
/// Interface for message serialization and deserialization
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// Serializes a message to a byte array
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to serialize</param>
    /// <returns>Serialized message as byte array</returns>
    byte[] Serialize<T>(T message) where T : class;

    /// <summary>
    /// Deserializes a byte array to a strongly-typed message
    /// </summary>
    /// <typeparam name="T">The expected message type</typeparam>
    /// <param name="data">The serialized message data</param>
    /// <returns>Deserialized message</returns>
    T Deserialize<T>(byte[] data) where T : class;

    /// <summary>
    /// Deserializes a byte array to a message using runtime type information
    /// </summary>
    /// <param name="data">The serialized message data</param>
    /// <param name="messageType">The runtime type of the message</param>
    /// <returns>Deserialized message</returns>
    object Deserialize(byte[] data, Type messageType);

    /// <summary>
    /// Gets the content type identifier for this serializer
    /// </summary>
    string ContentType { get; }
}
