using Microsoft.AspNetCore.SignalR;
using MultiVoice.Server.Auth;
using MultiVoice.Server.Players;
using MultiVoice.Server.Voice;

namespace MultiVoice.Server.Minecraft;

public class MinecraftHub(PluginSecret secret, MinecraftLink link, PlayerRegistry players, VoiceRouter voice, VoiceStates states, LoginService login) : Hub
{
    public override Task OnConnectedAsync()
    {
        if (!secret.Matches(Context.GetHttpContext()?.Request.Headers["X-Secret"]))
        {
            Context.Abort();
            return Task.CompletedTask;
        }
        players.Clear();
        link.Attach(Context.ConnectionId);
        return states.Refresh(states.Known());
    }

    public async Task Players(PlayerPosition[] changed, string[] left)
    {
        if (!link.Is(Context.ConnectionId)) return;
        players.Update(changed, left);
        await voice.PushListeners(changed);
        await states.Refresh(changed.Select(p => p.Xuid).Concat(left));
    }

    public Task Confirm(string xuid, int code) =>
        link.Is(Context.ConnectionId) ? login.Confirm(xuid, code) : Task.CompletedTask;

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (link.Detach(Context.ConnectionId))
        {
            players.Clear();
            await states.Refresh(states.Known());
        }
        await base.OnDisconnectedAsync(exception);
    }
}
