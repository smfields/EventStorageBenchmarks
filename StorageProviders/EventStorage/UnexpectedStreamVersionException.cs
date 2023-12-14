namespace EventStorageBenchmarks.StorageProviders.EventStorage;

public class UnexpectedStreamVersionException(int expectedVersion, int actualVersion) : Exception($"Expected stream version {expectedVersion} but was {actualVersion}");