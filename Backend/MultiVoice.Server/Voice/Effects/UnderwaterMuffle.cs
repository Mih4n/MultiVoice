using MultiVoice.Server.Voice.Pipeline;

namespace MultiVoice.Server.Voice.Effects;

public sealed class UnderwaterMuffle(float cutoff) : IVoiceEffect
{
    public void Apply(VoiceContext ctx)
    {
        if (ctx.From.InWater || ctx.To.InWater) ctx.Mix.Lowpass = Math.Min(ctx.Mix.Lowpass, cutoff);
    }
}
