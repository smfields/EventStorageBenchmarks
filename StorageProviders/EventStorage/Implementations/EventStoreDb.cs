using EventStore.Client;
using Testcontainers.EventStoreDb;

namespace EventStorageBenchmarks.StorageProviders.EventStorage.Implementations;

public class EventStoreDb : IEventStorage, IAsyncDisposable
{
    private EventStoreDbContainer? Container { get; set; }
    private EventStoreClient EventStoreClient { get; set; } = null!;
    
    public async Task InitializeAsync()
    {
        Container = new EventStoreDbBuilder().Build();
        await Container.StartAsync();

        var clientSettings = EventStoreClientSettings.Create(Container.GetConnectionString());
        EventStoreClient = new EventStoreClient(clientSettings);
    }

    public async Task AppendEventsAsync(string streamId, int expectedVersion, IEnumerable<byte[]> events)
    {
        var eventData = events
            .Select(@event => new EventData(Uuid.NewUuid(), "Event", @event));

        var expectedRevision = expectedVersion == 0 ? StreamRevision.None : (uint)(expectedVersion - 1);

        try
        {
            await EventStoreClient.AppendToStreamAsync(streamId, expectedRevision, eventData);
        }
        catch (WrongExpectedVersionException e)
        {
            var actualVersion = e.ActualVersion;
            throw new UnexpectedStreamVersionException(
                expectedVersion,
                actualVersion != null ? (int)actualVersion + 1 : -1
            );
        }
    }

    public async IAsyncEnumerable<byte[]> ReadEventsAsync(string streamId, int fromVersion = 0, int maxCount = int.MaxValue)
    {
        var events = EventStoreClient.ReadStreamAsync(Direction.Forwards, streamId, (uint)fromVersion, maxCount);

        if (await events.ReadState == ReadState.StreamNotFound)
        {
            throw new StreamNotFoundException(streamId);
        }

        await foreach (var @event in events)
        {
            yield return @event.Event.Data.ToArray();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Container is not null)
        {
            await Container.StopAsync();
            await Container.DisposeAsync();
        }
    }

    public override string ToString() => "EventStoreDb";
}