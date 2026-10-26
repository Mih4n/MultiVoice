using Microsoft.AspNetCore.SignalR;
using MultiVoice.Server.Auth;
using MultiVoice.Server.Players;
using MultiVoice.Server.Settings;

namespace MultiVoice.Server.Voice;

public class VoiceHub(LoginService login, VoiceSessions sessions, VoiceRouter router, VoiceStates states, SpeakerSettings settings, PlayerRegistry players) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var request = Context.GetHttpContext()?.Request;
        var token = request?.Query["access_token"].FirstOrDefault();
        token ??= request?.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");

        if (login.Resolve(token) is not { } user)
        {
            Context.Abort();
            return;
        }
        sessions.Bind(Context.ConnectionId, user.Xuid);
        await Clients.Caller.SendAsync("Settings", settings.Of(user.Xuid));
        await Clients.Caller.SendAsync("Players", states.Snapshot(user.Xuid));
        await states.Refresh([user.Xuid]);
    }

    public Task Voice(byte[] frame) =>
        sessions.XuidOf(Context.ConnectionId) is { } xuid ? router.Route(xuid, frame) : Task.CompletedTask;

    public Task State(bool mic, bool speaking)
    {
        if (sessions.XuidOf(Context.ConnectionId) is not { } xuid) return Task.CompletedTask;
        states.Report(xuid, mic, speaking);
        return states.Refresh([xuid]);
    }

    public Task SetVolume(string speakerXuid, float volume)
    {
        if (!float.IsFinite(volume)) throw new HubException("Неверная громкость");
        return ChangeSetting(speakerXuid, s => s with { Volume = Math.Clamp(volume, 0, SpeakerSettings.MaxVolume) });
    }

    public Task SetMuted(string speakerXuid, bool muted) => ChangeSetting(speakerXuid, s => s with { Muted = muted });

    Task ChangeSetting(string speakerXuid, Func<SpeakerSetting, SpeakerSetting> change)
    {
        if (sessions.XuidOf(Context.ConnectionId) is not { } me) throw new HubException("Нет голосовой сессии");
        if (speakerXuid == me || players.Get(speakerXuid) is null) throw new HubException("Игрок не в игре");
        return settings.Change(me, speakerXuid, change);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (sessions.Unbind(Context.ConnectionId) is { } xuid && sessions.ConnectionOf(xuid) is null)
        {
            states.Forget(xuid);
            await states.Refresh([xuid]);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
