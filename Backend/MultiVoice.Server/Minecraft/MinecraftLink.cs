using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace MultiVoice.Server.Minecraft;

public sealed class MinecraftLink(IHubContext<MinecraftHub> hub)
{
    // connection id -> server id; null is a plugin that sends no id
    readonly ConcurrentDictionary<string, string?> servers = new();

    public bool TryServer(string id, out string? server) => servers.TryGetValue(id, out server);

    // a reconnect of the same server replaces its old connection
    public void Attach(string id, string? server)
    {
        foreach (var old in servers.Where(s => s.Value == server)) servers.TryRemove(old);
        servers[id] = server;
    }

    public bool Detach(string id, out string? server) => servers.TryRemove(id, out server);

    public async Task<bool> Ask(string? server, string name, int[] codes)
    {
        if (Connection(server) is not { } id) return false;
        await hub.Clients.Client(id).SendAsync("Ask", name, codes);
        return true;
    }

    public Task VoiceStates(string? server, Dictionary<string, string> states) =>
        Connection(server) is { } id ? hub.Clients.Client(id).SendAsync("VoiceStates", states) : Task.CompletedTask;

    string? Connection(string? server) => servers.FirstOrDefault(s => s.Value == server).Key;
}
