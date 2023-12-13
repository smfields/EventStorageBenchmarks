namespace EventStorageBenchmarks.StorageProviders.Implementations;

public class Memory : IEventStorage
{
    private readonly Dictionary<string, List<byte[]>> _events = new();
    
    public Task AppendEventsAsync(string streamId, int expectedVersion, IEnumerable<byte[]> events)
    {
        _events.TryAdd(streamId, []);
            
        var streamEvents = _events[streamId];

        if (streamEvents.Count != expectedVersion)
        {
            throw new UnexpectedStreamVersionException(expectedVersion, streamEvents.Count);
        }
            
        streamEvents.AddRange(events);
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<byte[]> ReadEventsAsync(string streamId)
    {
        if (_events.TryGetValue(streamId, out var events))
        {
            return _events[streamId].ToAsyncEnumerable();
        }

        throw new StreamNotFoundException(streamId);
    }
    
    public override string ToString() => "Memory";
}