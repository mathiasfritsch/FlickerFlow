using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlickerFlow.Abstractions;

namespace FlickerFlow.Core.Serialization;

/// <summary>
/// Default message serializer using System.Text.Json
/// </summary>
public class SystemTextJsonSerializer : IMessageSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the SystemTextJsonSerializer
    /// </summary>
    public SystemTextJsonSerializer()
        : this(CreateDefaultOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the SystemTextJsonSerializer with custom options
    /// </summary>
    /// <param name="options">JSON serializer options</param>
    public SystemTextJsonSerializer(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string ContentType => "application/json";

    /// <inheritdoc />
    public byte[] Serialize<T>(T message) where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        try
        {
            var json = JsonSerializer.Serialize(message, _options);
            return Encoding.UTF8.GetBytes(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to serialize message of type {typeof(T).Name}", ex);
        }
    }

    /// <inheritdoc />
    public T Deserialize<T>(byte[] data) where T : class
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        try
        {
            var json = Encoding.UTF8.GetString(data);
            var message = JsonSerializer.Deserialize<T>(json, _options);
            
            if (message == null)
                throw new InvalidOperationException("Deserialization returned null");

            return message;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize message to type {typeof(T).Name}", ex);
        }
    }

    /// <inheritdoc />
    public object Deserialize(byte[] data, Type messageType)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        try
        {
            var json = Encoding.UTF8.GetString(data);
            var message = JsonSerializer.Deserialize(json, messageType, _options);
            
            if (message == null)
                throw new InvalidOperationException("Deserialization returned null");

            return message;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize message to type {messageType.Name}", ex);
        }
    }

    /// <summary>
    /// Creates default JSON serializer options
    /// </summary>
    /// <returns>Default JsonSerializerOptions</returns>
    private static JsonSerializerOptions CreateDefaultOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }
}
