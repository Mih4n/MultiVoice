using System.Collections.Concurrent;
using MultiVoice.Server.Chunks;
using MultiVoice.Server.Voice;

namespace MultiVoice.Server.Players;

public sealed class PlayerRegistry
{
    readonly int chunkSize;
    readonly ConcurrentDictionary<string, PlayerPosition> players = new();
    readonly ConcurrentDictionary<string, string> xuidsByName = new(StringComparer.OrdinalIgnoreCase);
    readonly Lock writeLock = new();

    public PlayerRegistry(VoiceItems items)
    {
        chunkSize = items.ChunkSize;
        World = new World([], chunkSize);
    }

    public World World { get; private set; }

    public PlayerPosition? Get(string xuid) => players.GetValueOrDefault(xuid);

    public string? XuidOf(string name) => xuidsByName.GetValueOrDefault(name);

    // returns the xuids that actually left: a player who moved to another server stays there
    public string[] Update(string? server, IEnumerable<PlayerPosition> changed, IEnumerable<string> left)
    {
        lock (writeLock)
        {
            var removed = new List<string>();
            foreach (var xuid in left)
                if (players.TryGetValue(xuid, out var gone) && gone.Server == server && players.TryRemove(xuid, out _))
                {
                    xuidsByName.TryRemove(new KeyValuePair<string, string>(gone.Name, xuid));
                    removed.Add(xuid);
                }

            // a player shown by another server too is moved to this one
            foreach (var player in changed.Select(p => p with { Server = server }))
            {
                if (players.TryGetValue(player.Xuid, out var old) && old.Name != player.Name)
                    xuidsByName.TryRemove(new KeyValuePair<string, string>(old.Name, player.Xuid));
                players[player.Xuid] = player;
                xuidsByName[player.Name] = player.Xuid;
            }

            World = new World(players.Values, chunkSize);
            return [.. removed];
        }
    }

    public void Clear(string? server) => Update(server, [], [.. players.Values.Where(p => p.Server == server).Select(p => p.Xuid)]);
}
