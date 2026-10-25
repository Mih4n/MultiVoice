using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.SignalR;
using MultiVoice.Server.Minecraft;
using MultiVoice.Server.Players;

namespace MultiVoice.Server.Auth;

public sealed class LoginService(PlayerRegistry players, MinecraftLink minecraft, IHubContext<AuthHub> auth)
{
    static readonly TimeSpan RequestCooldown = TimeSpan.FromSeconds(30);

    readonly ConcurrentDictionary<string, PendingLogin> pending = new();
    readonly ConcurrentDictionary<string, LoginToken> tokens = new();

    public async Task<int> RequestCode(string name, string connectionId)
    {
        name = name.Trim();
        if (players.XuidOf(name) is not { } xuid) throw new HubException("Игрок не в игре");
        if (pending.TryGetValue(xuid, out var old) && old.Created > DateTime.UtcNow - RequestCooldown)
            throw new HubException("Повторный запрос возможен через 30 секунд");

        var codes = Enumerable.Range(10, 90).ToArray();
        Random.Shared.Shuffle(codes);
        var correct = codes[0];
        var buttons = codes[..3];
        Random.Shared.Shuffle(buttons);

        pending[xuid] = new PendingLogin(connectionId, correct, DateTime.UtcNow);
        if (!await minecraft.Ask(players.Get(xuid)?.Name ?? name, buttons)) throw new HubException("Сервер Minecraft не подключён");
        return correct;
    }

    public async Task Confirm(string xuid, int code)
    {
        if (!pending.TryGetValue(xuid, out var login)) return;
        if (code == login.Code && players.Get(xuid) is { } player)
        {
            pending.TryRemove(xuid, out _);
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            tokens[token] = new LoginToken(xuid, player.Name);
            await auth.Clients.Client(login.ConnectionId).SendAsync("Confirmed", token);
        }
        else
        {
            pending[xuid] = login with { Code = -1 };
            await auth.Clients.Client(login.ConnectionId).SendAsync("Rejected");
        }
    }

    public LoginToken? Resolve(string? token) => token is not null ? tokens.GetValueOrDefault(token) : null;
}
