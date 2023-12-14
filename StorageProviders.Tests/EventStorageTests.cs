using System.Text.Json;
using EventStorageBenchmarks.StorageProviders;
using EventStorageBenchmarks.StorageProviders.EventStorage;

namespace StorageProviders.Tests;

public class EventStorageTests(IEventStorage storageProvider) : StorageProviderTestFixture(storageProvider)
{
    [Test]
    public void An_event_can_be_appended_to_a_non_existent_stream()
    {
        const string stringValue = "StringValue";
        const int integerValue = 10;
        var sampleEvent = new SampleEvent(stringValue, integerValue);
        var streamId = Guid.NewGuid().ToString();
        
        Assert.DoesNotThrowAsync(() => StorageProvider.AppendEventsAsync(
            streamId,
            0,
            [SerializeSampleEvent(sampleEvent)]
        ));
    }
    
    [Test]
    public void Retrieving_from_a_non_existent_throws_a_StreamNotFoundException()
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
            0,
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

        var expectedVersion = 0;
        foreach (var sampleEvent in events)
        {
            await StorageProvider.AppendEventsAsync(
                streamId,
                expectedVersion++,
                [SerializeSampleEvent(sampleEvent)]
            );
        }

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
            0,
            events.Select(SerializeSampleEvent)
        );

        var eventData = await StorageProvider.ReadEventsAsync(streamId).ToListAsync();
        var roundTripEvents = eventData.Select(DeserializeSampleEvent).ToList();
        Assert.That(roundTripEvents, Is.EquivalentTo(events));
    }

    [Test]
    public async Task Events_are_not_appended_if_the_expected_version_does_not_match()
    {
        var streamId = Guid.NewGuid().ToString();
        var event1 = new SampleEvent("Event1", 1);
        var event2 = new SampleEvent("Event2", 2);
        var event3 = new SampleEvent("Event3", 3);
        await StorageProvider.AppendEventsAsync(
            streamId,
            0,
            [SerializeSampleEvent(event1), SerializeSampleEvent(event2)]
        );

        try
        {
            await StorageProvider.AppendEventsAsync(streamId, 1, [SerializeSampleEvent(event3)]);
        }
        catch (Exception e)
        {
            Assert.That(e, Is.TypeOf<UnexpectedStreamVersionException>());
        }

        var storedEvents = await StorageProvider.ReadEventsAsync(streamId).ToListAsync();
        Assert.That(storedEvents, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Streams_can_be_read_starting_from_a_specified_version()
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
            0,
            events.Select(SerializeSampleEvent)
        );

        var readEvents = (await StorageProvider.ReadEventsAsync(streamId, 1).ToListAsync())
                         .Select(DeserializeSampleEvent)
                         .ToList();
        
        Assert.That(readEvents, Has.Count.EqualTo(2));
        Assert.That(readEvents.First(), Is.EqualTo(events[1]));
    }

    [Test]
    public async Task Max_count_can_be_used_to_control_the_number_of_events_returned()
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
            0,
            events.Select(SerializeSampleEvent)
        );

        var readEvents = (await StorageProvider.ReadEventsAsync(streamId, 0, 1).ToListAsync())
                         .Select(DeserializeSampleEvent)
                         .ToList();
        
        Assert.That(readEvents, Has.Count.EqualTo(1));
        Assert.That(readEvents.First(), Is.EqualTo(events[0]));
    }
}