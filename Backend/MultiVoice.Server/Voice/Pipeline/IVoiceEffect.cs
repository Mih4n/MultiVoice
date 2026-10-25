namespace MultiVoice.Server.Voice.Pipeline;

public interface IVoiceEffect
{
    void Apply(VoiceContext ctx);
}
