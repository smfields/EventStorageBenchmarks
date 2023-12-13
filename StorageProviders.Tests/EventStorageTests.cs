using System.Text.Json;
using EventStorageBenchmarks.StorageProviders;

namespace StorageProviders.Tests;


public class EventStorageTests(IEventStorage storageProvider) : StorageProviderTestFixture(storageProvider)
{
    [Test]
    public void An_event_can_be_appended_to_the_event_storage()
    {
        const string stringValue = "StringValue";
        const int integerValue = 10;
        var sampleEvent = new SampleEvent(stringValue, integerValue);
        var streamId = Guid.NewGuid().ToString();
        
        Assert.DoesNotThrowAsync(() => StorageProvider.AppendEventsAsync(
            streamId,
            [SerializeSampleEvent(sampleEvent)]
        ));
    }
    
    [Test]
    public void Retrieving_from_an_empty_stream_throws_a_StreamNotFoundException()
    {
        var streamId = Guid.NewGuid().ToString();

        Assert.ThrowsAsync<StreamNotFoundException>(() =>
        {
            var eventStream = StorageProvider.ReadEventsAsync(streamId);
            return eventStream.ToListAsync().AsTask();
        });
    }

    [Test]
    public async Task Events_are_equivalent_after_a_round_trip_to_the_event_storage()
    {
        const string stringValue = "StringValue";
        const int integerValue = 10;
        var sampleEvent = new SampleEvent(stringValue, integerValue);
        var streamId = Guid.NewGuid().ToString();

        await StorageProvider.AppendEventsAsync(
            streamId,
            [SerializeSampleEvent(sampleEvent)]
        );

        var events = await StorageProvider.ReadEventsAsync(streamId).ToListAsync();
        var roundTripEvent = DeserializeSampleEvent(events.First());
        Assert.That(roundTripEvent, Is.TypeOf<SampleEvent>());
        Assert.That(roundTripEvent, Is.EqualTo(sampleEvent));
    }

    [Test]
    public async Task Multiple_events_can_be_appended_sequentially()
    {
        List<SampleEvent> events =
        [
            new SampleEvent("Event1", 1),
            new SampleEvent("Event2", 2),
            new SampleEvent("Event3", 3),
        ];
        var streamId = Guid.NewGuid().ToString();

        foreach (var sampleEvent in events)
        {
            await StorageProvider.AppendEventsAsync(
                streamId,
                [SerializeSampleEvent(sampleEvent)]
            );
        }

        var eventData = await StorageProvider.ReadEventsAsync(streamId).ToListAsync();
        var roundTripEvents = eventData.Select(DeserializeSampleEvent).ToList();
        Assert.That(roundTripEvents, Is.EquivalentTo(events));
    }
    
    [Test]
    public async Task Multiple_events_can_be_appended_in_parallel()
    {
        List<SampleEvent> events =
        [
            new SampleEvent("Event1", 1),
            new SampleEvent("Event2", 2),
            new SampleEvent("Event3", 3),
        ];
        var streamId = Guid.NewGuid().ToString();

        var tasks = events
            .Select(sampleEvent => StorageProvider.AppendEventsAsync(streamId, [SerializeSampleEvent(sampleEvent)]))
            .ToList();
        await Task.WhenAll(tasks);

        var eventData = await StorageProvider.ReadEventsAsync(streamId).ToListAsync();
        var roundTripEvents = eventData.Select(DeserializeSampleEvent).ToList();
        Assert.That(roundTripEvents, Is.EquivalentTo(events));
    }
    
    [Test]
    public async Task Multiple_events_can_be_appended_in_a_single_batch()
    {
        List<SampleEvent> events =
        [
            new SampleEvent("Event1", 1),
            new SampleEvent("Event2", 2),
            new SampleEvent("Event3", 3),
        ];
        var streamId = Guid.NewGuid().ToString();

        await StorageProvider.AppendEventsAsync(
            streamId,
            events.Select(x => JsonSerializer.SerializeToUtf8Bytes(x))
        );

        var eventData = await StorageProvider.ReadEventsAsync(streamId).ToListAsync();
        var roundTripEvents = eventData.Select(x => JsonSerializer.Deserialize<SampleEvent>(x)).ToList();
        Assert.That(roundTripEvents, Is.EquivalentTo(events));
    }
}