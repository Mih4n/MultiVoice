using MultiVoice.Server.Players;
using MultiVoice.Server.Settings;

namespace MultiVoice.Server.Voice;

public sealed class Moderation(VoiceBans bans, PlayerRegistry players, VoiceRouter router, VoiceStates states)
{
    public bool IsAdmin(string xuid) => players.Get(xuid)?.Admin == true;

    public async Task Ban(string xuid, int? minutes)
    {
        await bans.Set(xuid, minutes is { } m ? DateTime.UtcNow.AddMinutes(m) : null);
        await Changed(xuid);
    }

    public async Task<bool> Unban(string xuid)
    {
        if (!await bans.Remove(xuid)) return false;
        await Changed(xuid);
        return true;
    }

    async Task Changed(string xuid)
    {
        if (players.Get(xuid) is { } p) await router.PushListeners([p]);
        await states.Refresh([xuid]);
    }
}
