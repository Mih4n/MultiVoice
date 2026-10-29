using Microsoft.AspNetCore.SignalR;
using MultiVoice.Server.Auth;
using MultiVoice.Server.Players;
using MultiVoice.Server.Voice;

namespace MultiVoice.Server.Minecraft;

public class MinecraftHub(PluginSecret secret, MinecraftLink link, PlayerRegistry players, VoiceRouter voice, VoiceStates states, LoginService login, RoomActions rooms, Moderation moderation) : Hub
{
    public override Task OnConnectedAsync()
    {
        var headers = Context.GetHttpContext()?.Request.Headers;
        if (!secret.Matches(headers?["X-Secret"]))
        {
            Context.Abort();
            return Task.CompletedTask;
        }
        // the plugin makes a new id on every start; an old plugin sends none and is the server null
        var server = headers?["X-Server"].FirstOrDefault();
        players.Clear(server);
        link.Attach(Context.ConnectionId, server);
        return states.Refresh(states.Known());
    }

    public async Task Players(PlayerPosition[] changed, string[] left)
    {
        if (!link.TryServer(Context.ConnectionId, out var server)) return;
        foreach (var xuid in players.Update(server, changed, left)) await rooms.Leave(xuid);
        await voice.PushListeners(changed);
        await states.Refresh(changed.Select(p => p.Xuid).Concat(left));
    }

    public Task Confirm(string xuid, int code) =>
        OnThisServer(xuid) ? login.Confirm(xuid, code) : Task.CompletedTask;

    public async Task Room(string xuid, string action, string? code)
    {
        if (!OnThisServer(xuid)) return;
        var reply = action switch
        {
            "create" => $"Комната создана, код: {await rooms.Create(xuid)}",
            "join" when code is not null => await rooms.Join(xuid, code) ? $"Вы в комнате {code.ToUpperInvariant()}" : "Комната не найдена",
            "leave" => await rooms.Leave(xuid) ? "Вы вышли из комнаты" : "Вы не в комнате",
            _ => "Использование: /room create | join <код> | leave",
        };
        await Clients.Caller.SendAsync("Tell", xuid, reply);
    }

    public async Task Ban(string adminXuid, string target, int? minutes)
    {
        if (!OnThisServer(adminXuid)) return;
        var reply = !moderation.IsAdmin(adminXuid) ? "Нужны права опа"
            : XuidOnThisServer(target) is not { } xuid ? $"{target} не в игре"
            : minutes is <= 0 ? "Неверный срок"
            : null;
        if (reply is null)
        {
            await moderation.Ban(XuidOnThisServer(target)!, minutes);
            reply = minutes is { } m ? $"{target} не может говорить {m} мин" : $"{target} не может говорить, пока запрет не снимут";
        }
        await Clients.Caller.SendAsync("Tell", adminXuid, reply);
    }

    public async Task Unban(string adminXuid, string target)
    {
        if (!OnThisServer(adminXuid)) return;
        var reply = !moderation.IsAdmin(adminXuid) ? "Нужны права опа"
            : XuidOnThisServer(target) is not { } xuid ? $"{target} не в игре"
            : await moderation.Unban(xuid) ? $"{target} снова может говорить"
            : $"У {target} нет запрета";
        await Clients.Caller.SendAsync("Tell", adminXuid, reply);
    }

    // a plugin acts only for players on its own server; a replaced connection for nobody
    bool OnThisServer(string xuid) =>
        link.TryServer(Context.ConnectionId, out var server) && players.Get(xuid) is { } p && p.Server == server;

    string? XuidOnThisServer(string name) => players.XuidOf(name) is { } xuid && OnThisServer(xuid) ? xuid : null;

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (link.Detach(Context.ConnectionId, out var server))
        {
            players.Clear(server);
            await states.Refresh(states.Known());
        }
        await base.OnDisconnectedAsync(exception);
    }
}
