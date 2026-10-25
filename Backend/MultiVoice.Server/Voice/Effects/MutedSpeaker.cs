using MultiVoice.Server.Voice.Pipeline;

namespace MultiVoice.Server.Voice.Effects;

public sealed class MutedSpeaker : IVoiceEffect
{
    public void Apply(VoiceContext ctx)
    {
        if (ctx.From.Muted) ctx.Drop();
    }
}
