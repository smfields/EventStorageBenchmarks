namespace EventStorageBenchmarks.StorageProviders.EventStorage.Implementations;

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

    public IAsyncEnumerable<byte[]> ReadEventsAsync(string streamId, int fromVersion = 0, int maxCount = int.MaxValue)
    {
        if (_events.TryGetValue(streamId, out var events))
        {
            maxCount = Math.Min(events.Count - fromVersion, maxCount);
            return events.GetRange(fromVersion, maxCount).ToAsyncEnumerable();
        }

        throw new StreamNotFoundException(streamId);
    }
    
    public override string ToString() => "Memory";
}