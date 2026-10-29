using Microsoft.AspNetCore.SignalR;
using MultiVoice.Server.Minecraft;
using MultiVoice.Server.Players;
using MultiVoice.Server.Voice;

namespace MultiVoice.Server.Dev;

public class DevHub(PluginSecret secret, PlayerRegistry players, VoiceRouter router) : Hub
{
    const string BotXuid = "VoiceBot";

    public PlayerPosition SpawnBot(string pluginSecret, string near)
    {
        if (!secret.Matches(pluginSecret)) throw new HubException("Неверный секрет");
        if (players.XuidOf(near) is not { } xuid || players.Get(xuid) is not { } p) throw new HubException($"{near} не в игре");
        var bot = p with { Xuid = BotXuid, Name = "VoiceBot", InWater = false, InCave = false, Muted = false };
        players.Update(p.Server, [bot], []);
        Context.Items[BotXuid] = true;
        return bot;
    }

    public Task Voice(byte[] frame) =>
        Context.Items.ContainsKey(BotXuid) ? router.Route(BotXuid, frame) : Task.CompletedTask;

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.ContainsKey(BotXuid)) players.Update(players.Get(BotXuid)?.Server, [], [BotXuid]);
        return base.OnDisconnectedAsync(exception);
    }
}
