using System.Text.Json;
using BenchmarkDotNet.Attributes;
using EventStorageBenchmarks.StorageProviders;

namespace EventStorageBenchmarks.Benchmarks;

public class AppendBenchmarks
{
    [ParamsSource(nameof(EventStorageProviders))]
    public IEventStorage EventStorage { get; set; } = null!;

    public IEnumerable<IEventStorage> EventStorageProviders() => new AllStorageProviders();
    
    [Params(1_000)]
    public int NumEvents { get; set; }

    private IEnumerable<byte[]> Events { get; set; } = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        Events = Enumerable
            .Range(0, NumEvents)
            .Select(i => JsonSerializer.SerializeToUtf8Bytes(new { Value = i }))
            .ToList();
        
        await EventStorage.InitializeAsync();
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
    public async Task AppendSequential()
    {
        foreach (var eventData in Events)
        {
            await EventStorage.AppendEventsAsync(
                Guid.NewGuid().ToString(),
                [eventData]
            );
        }
    }
    
    [Benchmark]
    public async Task AppendParallel()
    {
        var tasks = Events
            .Select(eventData => EventStorage.AppendEventsAsync(
                Guid.NewGuid().ToString(), 
                [eventData]
            ))
            .ToList();

        await Task.WhenAll(tasks);
    }
    
    [Benchmark]
    public async Task AppendBatch()
    {
        await EventStorage.AppendEventsAsync(
            Guid.NewGuid().ToString(),
            Events
        );
    }
}