namespace MultiVoice.Server.Options;

public sealed class VoiceItem
{
    public string Id { get; init; } = "";
    public int Range { get; init; }
    public float Volume { get; init; } = 1;
    public string[] Effects { get; init; } = [];
}
