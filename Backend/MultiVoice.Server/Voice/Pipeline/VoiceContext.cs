namespace MultiVoice.Server.Voice.Pipeline;

public sealed class VoiceContext(PlayerPosition from, PlayerPosition to)
{
    public PlayerPosition From { get; } = from;
    public PlayerPosition To { get; } = to;
    public double Distance { get; } = Math.Sqrt(
        Math.Pow(from.X - to.X, 2) + Math.Pow(from.Y - to.Y, 2) + Math.Pow(from.Z - to.Z, 2));

    public VoiceMix Mix { get; } = new();

    public bool Dropped { get; private set; }
    public void Drop() => Dropped = true;
}
