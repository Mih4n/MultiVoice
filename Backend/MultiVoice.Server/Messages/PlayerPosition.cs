namespace MultiVoice.Server;

public record PlayerPosition(
    string Xuid,
    string Name,
    double X,
    double Y,
    double Z,
    float Yaw,
    float Pitch,
    string DimensionId,
    bool InWater,
    bool InCave,
    bool Muted,
    string[]? Items = null,
    bool Admin = false,
    // set by the backend: the plugin connection the player is on, null for a plugin that sends no id
    string? Server = null
);
