using System.Collections;

namespace FlickerFlow.Abstractions;

/// <summary>
/// Collection of message headers
/// </summary>
public class Headers : IEnumerable<KeyValuePair<string, object>>
{
    private readonly Dictionary<string, object> _headers = new();

    /// <summary>
    /// Standard header constants
    /// </summary>
    public static class StandardHeaders
    {
        public const string MessageId = "MessageId";
        public const string CorrelationId = "CorrelationId";
        public const string Timestamp = "Timestamp";
        public const string MessageType = "MessageType";
    }

    /// <summary>
    /// Get or set a header value
    /// </summary>
    public object? this[string key]
    {
        get => _headers.TryGetValue(key, out var value) ? value : null;
        set
        {
            if (value == null)
                _headers.Remove(key);
            else
                _headers[key] = value;
        }
    }

    /// <summary>
    /// Try to get a header value
    /// </summary>
    public bool TryGetHeader(string key, out object? value)
    {
        return _headers.TryGetValue(key, out value);
    }

    /// <summary>
    /// Get a typed header value
    /// </summary>
    public T? Get<T>(string key, T? defaultValue = default)
    {
        if (_headers.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }

    /// <summary>
    /// Set a header value
    /// </summary>
    public void Set(string key, object value)
    {
        _headers[key] = value;
    }

    /// <summary>
    /// Remove a header
    /// </summary>
    public bool Remove(string key)
    {
        return _headers.Remove(key);
    }

    /// <summary>
    /// Check if a header exists
    /// </summary>
    public bool ContainsKey(string key)
    {
        return _headers.ContainsKey(key);
    }

    /// <summary>
    /// Get all header keys
    /// </summary>
    public IEnumerable<string> Keys => _headers.Keys;

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return _headers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
