namespace EventStorageBenchmarks.StorageProviders.Implementations;

public class Memory : IEventStorage
{
    private readonly Dictionary<string, List<byte[]>> _events = new();
    
    public Task AppendEventsAsync(string streamId, IEnumerable<byte[]> events)
    {
        _events.TryAdd(streamId, []);
        _events[streamId].AddRange(events);
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