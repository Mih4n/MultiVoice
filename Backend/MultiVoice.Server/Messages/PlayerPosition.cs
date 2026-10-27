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
    bool Admin = false
);
