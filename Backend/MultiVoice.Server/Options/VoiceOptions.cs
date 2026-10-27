namespace MultiVoice.Server.Options;

public sealed class VoiceOptions
{
    public const string Section = "Voice";

    public int DefaultRange { get; init; } = 32;
    public Dictionary<string, Dictionary<string, float>> Effects { get; init; } = [];
    public VoiceItem[] Items { get; init; } = [];
    public string McSecret { get; init; } = "";
    public bool DevHub { get; init; }
    public string[] Origins { get; init; } = [];
}
