using System.Collections.Concurrent;

namespace FlickerFlow.Abstractions;

/// <summary>
/// Cache for message type information to support polymorphic deserialization
/// </summary>
public static class MessageTypeCache
{
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new();
    private static readonly ConcurrentDictionary<Type, string> NameCache = new();

    /// <summary>
    /// Gets the fully qualified type name for a message type
    /// </summary>
    /// <param name="messageType">The message type</param>
    /// <returns>Fully qualified type name</returns>
    public static string GetTypeName(Type messageType)
    {
        return NameCache.GetOrAdd(messageType, type =>
        {
            // Use AssemblyQualifiedName for full type information including assembly
            return type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        });
    }

    /// <summary>
    /// Gets the Type from a fully qualified type name
    /// </summary>
    /// <param name="typeName">The fully qualified type name</param>
    /// <returns>The Type, or null if not found</returns>
    public static Type? GetType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        return TypeCache.GetOrAdd(typeName, name =>
        {
            // Try to load the type from the type name
            var type = Type.GetType(name);
            if (type != null)
                return type;

            // If not found, try searching loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(name);
                if (type != null)
                    return type;
            }

            throw new InvalidOperationException($"Unable to resolve message type: {name}");
        });
    }

    /// <summary>
    /// Clears the type cache (useful for testing)
    /// </summary>
    public static void Clear()
    {
        TypeCache.Clear();
        NameCache.Clear();
    }
}
