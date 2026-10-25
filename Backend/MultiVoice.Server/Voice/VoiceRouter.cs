using Microsoft.AspNetCore.SignalR;
using MultiVoice.Server.Players;
using MultiVoice.Server.Voice.Pipeline;

namespace MultiVoice.Server.Voice;

public sealed class VoiceRouter(PlayerRegistry players, VoiceSessions sessions, VoicePipeline pipeline, IHubContext<VoiceHub> hub)
{
    public Task Route(string speakerXuid, byte[] frame)
    {
        if (players.Get(speakerXuid) is not { } from) return Task.CompletedTask;

        var sends = new List<Task>();
        foreach (var to in players.World.Near(from))
        {
            if (to.Xuid == from.Xuid || sessions.ConnectionOf(to.Xuid) is not { } conn) continue;
            if (pipeline.Run(from, to) is not { } mix) continue;
            sends.Add(hub.Clients.Client(conn).SendAsync("Voice", from.Name, frame, from.X, from.Y, from.Z, mix));
        }
        return Task.WhenAll(sends);
    }

    public Task PushListeners(IEnumerable<PlayerPosition> changed) => Task.WhenAll(changed
        .Select(p => (p, conn: sessions.ConnectionOf(p.Xuid)))
        .Where(x => x.conn is not null)
        .Select(x => hub.Clients.Client(x.conn!).SendAsync("Self", x.p.X, x.p.Y, x.p.Z, x.p.Yaw, x.p.Pitch, x.p.Muted)));
}
