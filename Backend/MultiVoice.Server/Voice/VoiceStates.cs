using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using MultiVoice.Server.Minecraft;
using MultiVoice.Server.Players;
using MultiVoice.Server.Settings;

namespace MultiVoice.Server.Voice;

public sealed class VoiceStates(PlayerRegistry players, VoiceSessions sessions, VoiceBans bans, MinecraftLink minecraft, IHubContext<VoiceHub> hub)
{
    public const string Off = "off";

    readonly ConcurrentDictionary<string, (bool Mic, bool Speaking)> flags = new();
    readonly Dictionary<string, PlayerVoice> sent = new();
    readonly SemaphoreSlim pushLock = new(1, 1);

    public void Report(string xuid, bool mic, bool speaking) => flags[xuid] = (mic, speaking);

    public void Forget(string xuid) => flags.TryRemove(xuid, out _);

    public PlayerVoice[] Snapshot(string exceptXuid) => sessions.Xuids
        .Where(x => x != exceptXuid)
        .Select(Current)
        .Where(p => p.State != Off)
        .ToArray();

    public string[] Known()
    {
        lock (sent) return [.. sent.Keys];
    }

    public async Task Refresh(IEnumerable<string> xuids)
    {
        await pushLock.WaitAsync();
        try
        {
            var changes = new List<PlayerVoice>();
            lock (sent)
            {
                foreach (var xuid in xuids.Distinct())
                {
                    var now = Current(xuid);
                    var before = sent.GetValueOrDefault(xuid);
                    if (before is null ? now.State == Off : now == before) continue;
                    if (now.State == Off) sent.Remove(xuid); else sent[xuid] = now;
                    changes.Add(now.State == Off && before is not null ? now with { Name = before.Name } : now);
                }
            }
            if (changes.Count == 0) return;

            await minecraft.VoiceStates(changes.ToDictionary(p => p.Xuid, p => p.State));
            foreach (var change in changes)
            {
                var own = sessions.ConnectionOf(change.Xuid);
                await (own is null ? hub.Clients.All : hub.Clients.AllExcept(own)).SendAsync("PlayerState", change);
            }
        }
        finally
        {
            pushLock.Release();
        }
    }

    PlayerVoice Current(string xuid)
    {
        var player = players.Get(xuid);
        var name = player?.Name ?? "";
        if (player is null || sessions.ConnectionOf(xuid) is null) return new(xuid, name, Off);
        var (mic, speaking) = flags.GetValueOrDefault(xuid);
        // shortcut: an expired ban shows until the player's next position change or voice report
        var banned = bans.Active(xuid) is not null;
        return new(xuid, name, player.Muted || banned || !mic ? "listening" : speaking ? "speaking" : "mic", banned);
    }
}
