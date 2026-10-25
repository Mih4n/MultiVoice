using System.Collections.Concurrent;

namespace MultiVoice.Server.Voice;

public sealed class VoiceSessions
{
    readonly ConcurrentDictionary<string, string> xuids = new();
    readonly ConcurrentDictionary<string, string> connections = new();

    public void Bind(string connectionId, string xuid)
    {
        if (connections.TryGetValue(xuid, out var old)) xuids.TryRemove(old, out _);
        xuids[connectionId] = xuid;
        connections[xuid] = connectionId;
    }

    public void Unbind(string connectionId)
    {
        if (xuids.TryRemove(connectionId, out var xuid))
            connections.TryRemove(new KeyValuePair<string, string>(xuid, connectionId));
    }

    public string? XuidOf(string connectionId) => xuids.GetValueOrDefault(connectionId);

    public string? ConnectionOf(string xuid) => connections.GetValueOrDefault(xuid);
}
