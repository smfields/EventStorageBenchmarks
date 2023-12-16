using System.Text.Json;
using BenchmarkDotNet.Attributes;
using EventStorageBenchmarks.StorageProviders;

namespace EventStorageBenchmarks.Benchmarks;

public class AppendExistingBenchmarks
{
    [ParamsSource(nameof(EventStorageProviders))]
    public IEventStorage EventStorage { get; set; } = null!;

    public IEnumerable<IEventStorage> EventStorageProviders() => new AllEventStorageProviders();
    
    [Params(1, 5, 10, 100)]
    public int NumEvents { get; set; }

    private string ExistingStreamId { get; set; } = null!;
    private int NumExistingEvents { get; set; }

    private IEnumerable<byte[]> Events { get; set; } = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        Events = Enumerable
            .Range(0, NumEvents)
            .Select(i => JsonSerializer.SerializeToUtf8Bytes(new { Value = i }))
            .ToList();
        
        await EventStorage.InitializeAsync();
        
        var existingEvents = Enumerable
            .Range(0, 100)
            .Select(i => JsonSerializer.SerializeToUtf8Bytes(new { Value = i }))
            .ToList();
        
        ExistingStreamId = Guid.NewGuid().ToString();
        NumExistingEvents += 100;
            
        await EventStorage.AppendEventsAsync(
            ExistingStreamId,
            0,
            existingEvents
        );
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
    public async Task AppendExistingSequential()
    {
        foreach (var eventData in Events)
        {
            await EventStorage.AppendEventsAsync(
                ExistingStreamId,
                NumExistingEvents++,
                [eventData]
            );
        }
    }
    
    [Benchmark]
    public async Task AppendExistingBatch()
    {
        await EventStorage.AppendEventsAsync(
            ExistingStreamId,
            NumExistingEvents,
            Events
        );
        
        NumExistingEvents += NumEvents;
    }
}