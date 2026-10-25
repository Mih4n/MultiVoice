using MultiVoice.Server.Voice.Pipeline;

namespace MultiVoice.Server.Voice.Effects;

public sealed class CaveReverb(float wet, float decay) : IVoiceEffect
{
    public void Apply(VoiceContext ctx)
    {
        if (!ctx.From.InCave && !ctx.To.InCave) return;
        ctx.Mix.ReverbWet = Math.Max(ctx.Mix.ReverbWet, wet);
        ctx.Mix.ReverbDecay = decay;
    }
}
