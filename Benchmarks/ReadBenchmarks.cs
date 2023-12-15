using System.Text.Json;
using BenchmarkDotNet.Attributes;
using EventStorageBenchmarks.StorageProviders;

namespace EventStorageBenchmarks.Benchmarks;

public class ReadBenchmarks
{
    [ParamsSource(nameof(EventStorageProviders))]
    public IEventStorage EventStorage { get; set; } = null!;

    public IEnumerable<IEventStorage> EventStorageProviders() => new AllEventStorageProviders();
    
    [Params(1, 100, 1_000)]
    public int NumEvents { get; set; }

    private string StreamId { get; set; } = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        StreamId = Guid.NewGuid().ToString();
        
        var events = Enumerable
            .Range(0, NumEvents)
            .Select(i => JsonSerializer.SerializeToUtf8Bytes(new { Value = i }))
            .ToList();
        
        await EventStorage.InitializeAsync();
        await EventStorage.AppendEventsAsync(StreamId, 0, events);
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        switch (EventStorage)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }

    [Benchmark]
    public async Task<List<byte[]>> ReadFromStart()
    {
        return await EventStorage.ReadEventsAsync(StreamId).ToListAsync();
    }
}