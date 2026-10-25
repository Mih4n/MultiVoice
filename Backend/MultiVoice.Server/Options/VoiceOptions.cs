namespace MultiVoice.Server.Options;

public sealed class VoiceOptions
{
    public const string Section = "Voice";

    public int ChunkSize { get; init; } = 32;
    public string McSecret { get; init; } = "";
    public bool DevHub { get; init; }
    public string[] Origins { get; init; } = [];
}
