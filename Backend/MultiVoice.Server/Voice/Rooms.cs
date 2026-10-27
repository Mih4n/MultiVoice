using System.Security.Cryptography;

namespace MultiVoice.Server.Voice;

public sealed class Rooms
{
    const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    const int CodeLength = 6;

    readonly Dictionary<string, HashSet<string>> members = [];
    readonly Dictionary<string, string> roomOf = [];
    readonly Lock gate = new();

    public string? RoomOf(string xuid)
    {
        lock (gate) return roomOf.GetValueOrDefault(xuid);
    }

    public string[] Members(string code)
    {
        lock (gate) return members.TryGetValue(code, out var set) ? [.. set] : [];
    }

    public bool Together(string a, string b)
    {
        lock (gate) return roomOf.TryGetValue(a, out var room) && roomOf.GetValueOrDefault(b) == room;
    }

    public (string Code, string? Left) Create(string xuid)
    {
        lock (gate)
        {
            string code;
            do code = RandomNumberGenerator.GetString(Alphabet, CodeLength);
            while (members.ContainsKey(code));
            var left = LeaveLocked(xuid);
            members[code] = [xuid];
            roomOf[xuid] = code;
            return (code, left);
        }
    }

    public (bool Joined, string? Left) Join(string xuid, string code)
    {
        code = code.Trim().ToUpperInvariant();
        lock (gate)
        {
            if (!members.TryGetValue(code, out var set)) return (false, null);
            if (roomOf.GetValueOrDefault(xuid) == code) return (true, null);
            var left = LeaveLocked(xuid);
            set.Add(xuid);
            roomOf[xuid] = code;
            return (true, left);
        }
    }

    public string? Leave(string xuid)
    {
        lock (gate) return LeaveLocked(xuid);
    }

    string? LeaveLocked(string xuid)
    {
        if (!roomOf.Remove(xuid, out var code)) return null;
        var set = members[code];
        set.Remove(xuid);
        if (set.Count == 0) members.Remove(code);
        return code;
    }
}
