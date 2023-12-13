using System.Text.RegularExpressions;
using Marten;
using Marten.Events;
using Marten.Exceptions;
using Polly;
using Polly.Retry;
using Testcontainers.PostgreSql;
using Weasel.Core;

namespace EventStorageBenchmarks.StorageProviders.Implementations;

public class MartenDb : IEventStorage, IAsyncDisposable
{
    private PostgreSqlContainer? Container { get; set; }
    private DocumentStore Db { get; set; } = null!;

    public async Task InitializeAsync()
    {
        Container = new PostgreSqlBuilder().Build();
        await Container.StartAsync();

        Db = DocumentStore.For(opts =>
        {
            opts.Connection(Container.GetConnectionString());
            opts.AutoCreateSchemaObjects = AutoCreate.All;
            opts.Events.StreamIdentity = StreamIdentity.AsString;
        });
    }

    public async Task AppendEventsAsync(string streamId, int expectedVersion, IEnumerable<byte[]> events)
    {
        var eventList = events.ToList();
        
        await using var session = Db.LightweightSession();

        if (expectedVersion == 0)
        {
            session.Events.StartStream(streamId, eventList);
        }
        else
        {
            session.Events.Append(streamId, expectedVersion + eventList.Count, events: eventList);
        }

        try
        {
            await session.SaveChangesAsync();
        }
        catch (EventStreamUnexpectedMaxEventIdException e)
        {
            var actualVersionIndex = e.Message.LastIndexOf("but was ", StringComparison.Ordinal) + "but was ".Length;
            var actualVersion = int.Parse(e.Message[actualVersionIndex..]);
            throw new UnexpectedStreamVersionException(expectedVersion, actualVersion);
        }
    }

    public async IAsyncEnumerable<byte[]> ReadEventsAsync(string streamId)
    {
        await using var session = Db.LightweightSession();
        var events = await session.Events.FetchStreamAsync(streamId);

        if (events.IsEmpty())
        {
            throw new StreamNotFoundException(streamId);
        }
        
        foreach (var @event in events)
        {
            yield return (byte[])@event.Data;
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

    public override string ToString() => "MartenDb";
}