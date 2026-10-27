using Microsoft.AspNetCore.SignalR;
using MultiVoice.Server.Players;

namespace MultiVoice.Server.Voice;

public sealed class RoomActions(Rooms rooms, PlayerRegistry players, VoiceSessions sessions, IHubContext<VoiceHub> hub)
{
    public async Task<string> Create(string xuid)
    {
        var (code, left) = rooms.Create(xuid);
        await Changed(xuid, left);
        return code;
    }

    public async Task<bool> Join(string xuid, string code)
    {
        var (joined, left) = rooms.Join(xuid, code);
        if (joined) await Changed(xuid, left);
        return joined;
    }

    public async Task<bool> Leave(string xuid)
    {
        if (rooms.Leave(xuid) is not { } left) return false;
        await Changed(xuid, left);
        return true;
    }

    public Task Push(string xuid)
    {
        if (sessions.ConnectionOf(xuid) is not { } conn) return Task.CompletedTask;
        var code = rooms.RoomOf(xuid);
        string[] names = code is null ? [] : [.. rooms.Members(code).Select(x => players.Get(x)?.Name ?? sessions.NameOf(x) ?? x).Order()];
        return hub.Clients.Client(conn).SendAsync("Room", code, names);
    }

    async Task Changed(string xuid, string? left)
    {
        var targets = new HashSet<string> { xuid };
        if (left is not null) targets.UnionWith(rooms.Members(left));
        if (rooms.RoomOf(xuid) is { } now) targets.UnionWith(rooms.Members(now));
        foreach (var target in targets) await Push(target);
    }
}
