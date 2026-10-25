using Microsoft.AspNetCore.SignalR;
using MultiVoice.Server.Auth;
using MultiVoice.Server.Players;
using MultiVoice.Server.Voice;

namespace MultiVoice.Server.Minecraft;

public class MinecraftHub(PluginSecret secret, MinecraftLink link, PlayerRegistry players, VoiceRouter voice, LoginService login) : Hub
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
        return Task.CompletedTask;
    }

    public Task Players(PlayerPosition[] changed, string[] left)
    {
        if (!link.Is(Context.ConnectionId)) return Task.CompletedTask;
        players.Update(changed, left);
        return voice.PushListeners(changed);
    }

    public Task Confirm(string xuid, int code) =>
        link.Is(Context.ConnectionId) ? login.Confirm(xuid, code) : Task.CompletedTask;

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (link.Detach(Context.ConnectionId)) players.Clear();
        return base.OnDisconnectedAsync(exception);
    }
}
