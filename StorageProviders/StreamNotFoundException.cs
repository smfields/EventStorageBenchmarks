namespace EventStorageBenchmarks.StorageProviders;

public class StreamNotFoundException(string streamId) : Exception($"Event stream '{streamId}' was not found.");