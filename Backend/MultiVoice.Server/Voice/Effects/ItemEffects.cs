using MultiVoice.Server.Voice.Pipeline;

namespace MultiVoice.Server.Voice.Effects;

public sealed class ItemEffects(VoiceItems items) : IVoiceEffect
{
    public void Apply(VoiceContext ctx)
    {
        if (items.Strongest(ctx.From) is { } item) items.Apply(item, ctx.Mix);
    }
}
