namespace MultiVoice.Server.Voice.Pipeline;

public sealed class VoiceMix
{
    public float Volume { get; set; } = 1;
    public float Lowpass { get; set; } = 20000;
    public float Highpass { get; set; } = 0;
    public float ReverbWet { get; set; } = 0;
    public float ReverbDecay { get; set; } = 2;
    public float EchoDelay { get; set; } = 0;
    public float EchoFeedback { get; set; } = 0;
    public float EchoWet { get; set; } = 0;
}
