namespace EventStorageBenchmarks.StorageProviders;

public interface IEventStorage
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task AppendEventsAsync(string streamId, int expectedVersion, IEnumerable<byte[]> events);
    public IAsyncEnumerable<byte[]> ReadEventsAsync(string streamId, int fromVersion = 0, int maxCount = int.MaxValue);
}