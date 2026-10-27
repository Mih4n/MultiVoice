using MultiVoice.Server.Voice.Pipeline;

namespace MultiVoice.Server.Voice.Effects;

public sealed class DistanceFade(double fadeStart, VoiceItems items) : IVoiceEffect
{
    public void Apply(VoiceContext ctx)
    {
        double to = items.RangeOf(ctx.From), from = to * fadeStart;
        if (ctx.From.DimensionId != ctx.To.DimensionId || ctx.Distance >= to)
        {
            ctx.Drop();
            return;
        }
        if (ctx.Distance > from) ctx.Mix.Volume *= (float)((to - ctx.Distance) / (to - from));
    }
}
