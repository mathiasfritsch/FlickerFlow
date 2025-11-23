using System.Collections.Concurrent;
using System.Threading.Channels;
using FlickerFlow.Abstractions;

namespace FlickerFlow.Transports.InMemory;

/// <summary>
/// In-memory queue for message storage and delivery
/// </summary>
internal class InMemoryQueue
{
    private readonly Channel<MessageEnvelope> _channel;

    public InMemoryQueue(string name)
    {
        Name = name;
        _channel = Channel.CreateUnbounded<MessageEnvelope>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public string Name { get; }

    /// <summary>
    /// Enqueue a message
    /// </summary>
    public async Task Enqueue(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(envelope, cancellationToken);
    }

    /// <summary>
    /// Try to dequeue a message
    /// </summary>
    public async Task<MessageEnvelope?> TryDequeue(CancellationToken cancellationToken = default)
    {
        if (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            if (_channel.Reader.TryRead(out var envelope))
            {
                return envelope;
            }
        }

        return null;
    }

    /// <summary>
    /// Get the reader for consuming messages
    /// </summary>
    public ChannelReader<MessageEnvelope> Reader => _channel.Reader;
}
