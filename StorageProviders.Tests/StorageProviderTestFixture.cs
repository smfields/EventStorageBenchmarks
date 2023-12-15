using System.Text.Json;
using EventStorageBenchmarks.StorageProviders;

namespace StorageProviders.Tests;

[TestFixtureSource(typeof(AllEventStorageProviders))]
public abstract class StorageProviderTestFixture(IEventStorage storageProvider)
{
    protected IEventStorage StorageProvider { get; } = storageProvider;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        await StorageProvider.InitializeAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        switch (StorageProvider)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }
    
    protected byte[] SerializeSampleEvent(SampleEvent sampleEvent)
    {
        return JsonSerializer.SerializeToUtf8Bytes(sampleEvent);
    }

    protected SampleEvent? DeserializeSampleEvent(byte[] bytes)
    {
        return JsonSerializer.Deserialize<SampleEvent>(bytes);
    }

    public record SampleEvent(string StringValue, int IntegerValue);
}