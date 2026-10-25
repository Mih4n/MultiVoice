using MultiVoice.Server.Voice.Pipeline;

namespace MultiVoice.Server.Voice.Effects;

public sealed class DeepCaveEcho(double belowY, float delay, float feedback, float wet) : IVoiceEffect
{
    public void Apply(VoiceContext ctx)
    {
        if (!ctx.From.InCave || ctx.From.Y >= belowY) return;
        ctx.Mix.EchoDelay = delay;
        ctx.Mix.EchoFeedback = feedback;
        ctx.Mix.EchoWet = wet;
    }
}
