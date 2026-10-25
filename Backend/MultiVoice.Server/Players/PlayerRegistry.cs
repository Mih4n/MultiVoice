using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MultiVoice.Server.Chunks;
using MultiVoice.Server.Options;

namespace MultiVoice.Server.Players;

public sealed class PlayerRegistry
{
    readonly int chunkSize;
    readonly ConcurrentDictionary<string, PlayerPosition> players = new();
    readonly ConcurrentDictionary<string, string> xuidsByName = new(StringComparer.OrdinalIgnoreCase);
    readonly Lock writeLock = new();

    public PlayerRegistry(IOptions<VoiceOptions> options)
    {
        chunkSize = options.Value.ChunkSize;
        World = new World([], chunkSize);
    }

    public World World { get; private set; }

    public PlayerPosition? Get(string xuid) => players.GetValueOrDefault(xuid);

    public string? XuidOf(string name) => xuidsByName.GetValueOrDefault(name);

    public void Update(IEnumerable<PlayerPosition> changed, IEnumerable<string> left)
    {
        lock (writeLock)
        {
            foreach (var xuid in left)
                if (players.TryRemove(xuid, out var gone))
                    xuidsByName.TryRemove(new KeyValuePair<string, string>(gone.Name, xuid));

            foreach (var player in changed)
            {
                if (players.TryGetValue(player.Xuid, out var old) && old.Name != player.Name)
                    xuidsByName.TryRemove(new KeyValuePair<string, string>(old.Name, player.Xuid));
                players[player.Xuid] = player;
                xuidsByName[player.Name] = player.Xuid;
            }

            World = new World(players.Values, chunkSize);
        }
    }

    public void Clear() => Update([], players.Keys);
}
