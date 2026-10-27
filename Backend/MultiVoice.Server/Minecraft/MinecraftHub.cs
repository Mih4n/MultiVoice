using Microsoft.AspNetCore.SignalR;
using MultiVoice.Server.Auth;
using MultiVoice.Server.Players;
using MultiVoice.Server.Voice;

namespace MultiVoice.Server.Minecraft;

public class MinecraftHub(PluginSecret secret, MinecraftLink link, PlayerRegistry players, VoiceRouter voice, VoiceStates states, LoginService login, RoomActions rooms, Moderation moderation) : Hub
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
        foreach (var xuid in left) await rooms.Leave(xuid);
        await voice.PushListeners(changed);
        await states.Refresh(changed.Select(p => p.Xuid).Concat(left));
    }

    public Task Confirm(string xuid, int code) =>
        link.Is(Context.ConnectionId) ? login.Confirm(xuid, code) : Task.CompletedTask;

    public async Task Room(string xuid, string action, string? code)
    {
        if (!link.Is(Context.ConnectionId)) return;
        var reply = action switch
        {
            "create" => $"Комната создана, код: {await rooms.Create(xuid)}",
            "join" when code is not null => await rooms.Join(xuid, code) ? $"Вы в комнате {code.ToUpperInvariant()}" : "Комната не найдена",
            "leave" => await rooms.Leave(xuid) ? "Вы вышли из комнаты" : "Вы не в комнате",
            _ => "Использование: /room create | join <код> | leave",
        };
        await link.Tell(xuid, reply);
    }

    public async Task Ban(string adminXuid, string target, int? minutes)
    {
        if (!link.Is(Context.ConnectionId)) return;
        var reply = !moderation.IsAdmin(adminXuid) ? "Нужны права опа"
            : players.XuidOf(target) is not { } xuid ? $"{target} не в игре"
            : minutes is <= 0 ? "Неверный срок"
            : null;
        if (reply is null)
        {
            await moderation.Ban(players.XuidOf(target)!, minutes);
            reply = minutes is { } m ? $"{target} не может говорить {m} мин" : $"{target} не может говорить, пока запрет не снимут";
        }
        await link.Tell(adminXuid, reply);
    }

    public async Task Unban(string adminXuid, string target)
    {
        if (!link.Is(Context.ConnectionId)) return;
        var reply = !moderation.IsAdmin(adminXuid) ? "Нужны права опа"
            : players.XuidOf(target) is not { } xuid ? $"{target} не в игре"
            : await moderation.Unban(xuid) ? $"{target} снова может говорить"
            : $"У {target} нет запрета";
        await link.Tell(adminXuid, reply);
    }

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
