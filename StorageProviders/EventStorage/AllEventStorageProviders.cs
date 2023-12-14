using System.Collections;
using EventStorageBenchmarks.StorageProviders.EventStorage.Implementations;

namespace EventStorageBenchmarks.StorageProviders.EventStorage;

public class AllEventStorageProviders : IEnumerable<IEventStorage>
{
    public IEnumerator<IEventStorage> GetEnumerator()
    {
        yield return new Memory();
        yield return new EventStoreDb();
        yield return new MartenDb();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}