using Microsoft.AspNetCore.SignalR;

namespace MultiVoice.Server.Minecraft;

public sealed class MinecraftLink(IHubContext<MinecraftHub> hub)
{
    string? connectionId;

    public bool Is(string id) => id == connectionId;

    public void Attach(string id) => connectionId = id;

    public bool Detach(string id) => Interlocked.CompareExchange(ref connectionId, null, id) == id;

    public async Task<bool> Ask(string name, int[] codes)
    {
        if (connectionId is not { } id) return false;
        await hub.Clients.Client(id).SendAsync("Ask", name, codes);
        return true;
    }

    public Task VoiceStates(Dictionary<string, string> states) =>
        connectionId is { } id ? hub.Clients.Client(id).SendAsync("VoiceStates", states) : Task.CompletedTask;

    public Task Tell(string xuid, string message) =>
        connectionId is { } id ? hub.Clients.Client(id).SendAsync("Tell", xuid, message) : Task.CompletedTask;
}
