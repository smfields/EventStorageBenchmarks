using System.Collections;
using EventStorageBenchmarks.StorageProviders.Implementations;

namespace EventStorageBenchmarks.StorageProviders;

public class AllEventStorageProviders : IEnumerable<IEventStorage>
{
    public IEnumerator<IEventStorage> GetEnumerator()
    {
        // yield return new Memory();
        yield return new EventStoreDb();
        yield return new MartenDb();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}