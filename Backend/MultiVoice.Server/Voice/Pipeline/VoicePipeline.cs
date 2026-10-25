namespace MultiVoice.Server.Voice.Pipeline;

public sealed class VoicePipeline(IReadOnlyList<IVoiceEffect> effects)
{
    public VoiceMix? Run(PlayerPosition from, PlayerPosition to)
    {
        var ctx = new VoiceContext(from, to);
        foreach (var effect in effects)
        {
            effect.Apply(ctx);
            if (ctx.Dropped) return null;
        }
        return ctx.Mix;
    }
}
