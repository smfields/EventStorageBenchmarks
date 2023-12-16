using EventStore.Client;
using Testcontainers.EventStoreDb;

namespace EventStorageBenchmarks.StorageProviders.Implementations;

public class EventStoreDb : IEventStorage, IAsyncDisposable
{
    private EventStoreDbContainer? Container { get; set; }
    private EventStoreClient EventStoreClient { get; set; } = null!;
    
    public async Task InitializeAsync()
    {
        Container = new EventStoreDbBuilder().WithImage("eventstore/eventstore:lts").Build();
        await Container.StartAsync();

        var clientSettings = EventStoreClientSettings.Create(Container.GetConnectionString());
        EventStoreClient = new EventStoreClient(clientSettings);
    }

    public async Task AppendEventsAsync(string streamId, int expectedVersion, IEnumerable<byte[]> events)
    {
        var eventData = events
            .Select(@event => new EventData(Uuid.NewUuid(), "Event", @event));

        var expectedRevision =
            expectedVersion == 0 ? Expected.NoStream : Expected.FromRevision((uint)(expectedVersion - 1));

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

public abstract record Expected {
    public static readonly Expected NoStream = new State(StreamState.NoStream);
    public static readonly Expected StreamExists = new State(StreamState.StreamExists);
    public static readonly Expected Any = new State(StreamState.Any);

    public static Expected FromRevision(StreamRevision streamRevision) => new Revision(streamRevision);

    internal sealed record State(StreamState StreamState) : Expected;

    internal sealed record Revision(StreamRevision StreamRevision) : Expected;
}

public static class EventStoreClientExtensions {
    public static Task<IWriteResult> AppendToStreamAsync(this EventStoreClient client, string streamName,
        Expected expected,
        IEnumerable<EventData> eventData,
        Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
        TimeSpan? deadline = null,
        UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default) => expected switch {
        Expected.State(var state) => client.AppendToStreamAsync(streamName, state, eventData,
            configureOperationOptions, deadline, userCredentials, cancellationToken),
        Expected.Revision(var revision) => client.AppendToStreamAsync(streamName, revision, eventData,
            configureOperationOptions, deadline, userCredentials, cancellationToken),
        _ => throw new ArgumentException($"{expected.GetType()} not expected", nameof(expected))
    };
}