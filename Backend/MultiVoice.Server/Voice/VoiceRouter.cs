using Microsoft.AspNetCore.SignalR;
using MultiVoice.Server.Players;
using MultiVoice.Server.Settings;
using MultiVoice.Server.Voice.Pipeline;

namespace MultiVoice.Server.Voice;

public sealed class VoiceRouter(PlayerRegistry players, VoiceSessions sessions, VoicePipeline pipeline, SpeakerSettings settings, VoiceBans bans, Rooms rooms, IHubContext<VoiceHub> hub)
{
    public Task Route(string speakerXuid, byte[] frame)
    {
        if (players.Get(speakerXuid) is not { } from || from.Muted || bans.Active(from.Xuid) is not null) return Task.CompletedTask;

        var sends = new List<Task>();
        void Send(string xuid, Func<VoiceMix?> mixer)
        {
            if (xuid == from.Xuid || sessions.ConnectionOf(xuid) is not { } conn) return;
            var setting = settings.Get(xuid, from.Xuid);
            if (setting?.Muted == true || mixer() is not { } mix) return;
            if (setting is not null) mix.Volume *= setting.Volume;
            sends.Add(hub.Clients.Client(conn).SendAsync("Voice", from.Name, frame, from.X, from.Y, from.Z, mix));
        }

        if (rooms.RoomOf(from.Xuid) is { } room)
            foreach (var xuid in rooms.Members(room))
                Send(xuid, () => new VoiceMix { Room = true });

        foreach (var to in players.World.Near(from))
            if (!rooms.Together(from.Xuid, to.Xuid))
                Send(to.Xuid, () => pipeline.Run(from, to));

        return Task.WhenAll(sends);
    }

    public Task PushListeners(IEnumerable<PlayerPosition> changed) => Task.WhenAll(changed
        .Select(p => (p, conn: sessions.ConnectionOf(p.Xuid)))
        .Where(x => x.conn is not null)
        .Select(x => hub.Clients.Client(x.conn!).SendAsync("Self", x.p.X, x.p.Y, x.p.Z, x.p.Yaw, x.p.Pitch, x.p.Muted, x.p.Admin, BanOf(x.p.Xuid))));

    long? BanOf(string xuid) => bans.Active(xuid) is { } ban
        ? ban.Until is { } until ? new DateTimeOffset(until, TimeSpan.Zero).ToUnixTimeMilliseconds() : 0
        : null;
}
