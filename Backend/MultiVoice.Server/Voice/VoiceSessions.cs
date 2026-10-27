using System.Collections.Concurrent;

namespace MultiVoice.Server.Voice;

public sealed class VoiceSessions
{
    readonly ConcurrentDictionary<string, string> xuids = new();
    readonly ConcurrentDictionary<string, string> connections = new();
    readonly ConcurrentDictionary<string, string> names = new();

    public void Bind(string connectionId, string xuid, string name)
    {
        names[xuid] = name;
        if (connections.TryGetValue(xuid, out var old)) xuids.TryRemove(old, out _);
        xuids[connectionId] = xuid;
        connections[xuid] = connectionId;
    }

    public string? Unbind(string connectionId)
    {
        if (!xuids.TryRemove(connectionId, out var xuid)) return null;
        connections.TryRemove(new KeyValuePair<string, string>(xuid, connectionId));
        return xuid;
    }

    public string? XuidOf(string connectionId) => xuids.GetValueOrDefault(connectionId);

    public ICollection<string> Xuids => connections.Keys;

    // the name from the login token: the site can be open while the player is not in game
    public string? NameOf(string xuid) => names.GetValueOrDefault(xuid);

    public string? ConnectionOf(string xuid) => connections.GetValueOrDefault(xuid);
}
