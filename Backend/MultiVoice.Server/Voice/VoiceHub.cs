using Microsoft.AspNetCore.SignalR;
using MultiVoice.Server.Auth;

namespace MultiVoice.Server.Voice;

public class VoiceHub(LoginService login, VoiceSessions sessions, VoiceRouter router) : Hub
{
    public override Task OnConnectedAsync()
    {
        var request = Context.GetHttpContext()?.Request;
        var token = request?.Query["access_token"].FirstOrDefault();
        token ??= request?.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");

        if (login.Resolve(token) is not { } user)
        {
            Context.Abort();
            return Task.CompletedTask;
        }
        sessions.Bind(Context.ConnectionId, user.Xuid);
        return Task.CompletedTask;
    }

    public Task Voice(byte[] frame) =>
        sessions.XuidOf(Context.ConnectionId) is { } xuid ? router.Route(xuid, frame) : Task.CompletedTask;

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        sessions.Unbind(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
