namespace MultiVoice.Server.Settings;

public record SpeakerSetting(string ListenerXuid, string SpeakerXuid, float Volume, bool Muted);
