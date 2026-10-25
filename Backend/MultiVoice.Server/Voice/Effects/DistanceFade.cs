using MultiVoice.Server.Voice.Pipeline;

namespace MultiVoice.Server.Voice.Effects;

public sealed class DistanceFade(double from, double to) : IVoiceEffect
{
    public void Apply(VoiceContext ctx)
    {
        if (ctx.From.DimensionId != ctx.To.DimensionId || ctx.Distance >= to)
        {
            ctx.Drop();
            return;
        }
        if (ctx.Distance > from) ctx.Mix.Volume *= (float)((to - ctx.Distance) / (to - from));
    }
}
