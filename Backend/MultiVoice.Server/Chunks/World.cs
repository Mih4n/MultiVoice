namespace MultiVoice.Server.Chunks;

public sealed class World
{
    readonly Dictionary<(string? Server, string Dim, int X, int Y, int Z), List<PlayerPosition>> cells = [];
    readonly int size;

    public World(IEnumerable<PlayerPosition> players, int size)
    {
        this.size = size;
        foreach (var p in players)
        {
            var key = Cell(p);
            if (!cells.TryGetValue(key, out var list)) cells[key] = list = [];
            list.Add(p);
        }
    }

    public IEnumerable<PlayerPosition> Near(PlayerPosition p)
    {
        var (server, dim, cx, cy, cz) = Cell(p);
        for (var x = cx - 1; x <= cx + 1; x++)
        for (var y = cy - 1; y <= cy + 1; y++)
        for (var z = cz - 1; z <= cz + 1; z++)
            if (cells.TryGetValue((server, dim, x, y, z), out var list))
                foreach (var other in list) yield return other;
    }

    // each Minecraft server is its own world
    (string?, string, int, int, int) Cell(PlayerPosition p) =>
        (p.Server, p.DimensionId, (int)Math.Floor(p.X / size), (int)Math.Floor(p.Y / size), (int)Math.Floor(p.Z / size));
}
