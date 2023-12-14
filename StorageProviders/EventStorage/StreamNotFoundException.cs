namespace EventStorageBenchmarks.StorageProviders.EventStorage;

public class StreamNotFoundException(string streamId) : Exception($"Event stream '{streamId}' was not found.");