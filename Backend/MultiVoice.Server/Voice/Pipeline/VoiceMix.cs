namespace MultiVoice.Server.Voice.Pipeline;

public sealed class VoiceMix
{
    public static readonly IReadOnlyDictionary<string, Action<VoiceMix, float>> Keys = new Dictionary<string, Action<VoiceMix, float>>
    {
        [nameof(Lowpass)] = (m, v) => m.Lowpass = v,
        [nameof(Highpass)] = (m, v) => m.Highpass = v,
        [nameof(ReverbWet)] = (m, v) => m.ReverbWet = v,
        [nameof(ReverbDecay)] = (m, v) => m.ReverbDecay = v,
        [nameof(EchoDelay)] = (m, v) => m.EchoDelay = v,
        [nameof(EchoFeedback)] = (m, v) => m.EchoFeedback = v,
        [nameof(EchoWet)] = (m, v) => m.EchoWet = v,
        [nameof(Distortion)] = (m, v) => m.Distortion = v,
        [nameof(Compressor)] = (m, v) => m.Compressor = v,
        [nameof(Pitch)] = (m, v) => m.Pitch = v,
    };

    public float Volume { get; set; } = 1;
    public float Lowpass { get; set; } = 20000;
    public float Highpass { get; set; } = 0;
    public float ReverbWet { get; set; } = 0;
    public float ReverbDecay { get; set; } = 2;
    public float EchoDelay { get; set; } = 0;
    public float EchoFeedback { get; set; } = 0;
    public float EchoWet { get; set; } = 0;
    public float Distortion { get; set; } = 0;
    public float Compressor { get; set; } = 0;
    public float Pitch { get; set; } = 0;
    public bool Room { get; set; }
}
