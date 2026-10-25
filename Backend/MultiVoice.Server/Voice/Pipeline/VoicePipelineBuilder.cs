namespace MultiVoice.Server.Voice.Pipeline;

public sealed class VoicePipelineBuilder
{
    readonly List<IVoiceEffect> effects = [];

    public VoicePipelineBuilder Use(IVoiceEffect effect)
    {
        effects.Add(effect);
        return this;
    }

    public VoicePipeline Build() => new([.. effects]);
}
