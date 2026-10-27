using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace MultiVoice.Server.Settings;

public sealed class VoiceBans(IDbContextFactory<SettingsDb> dbs)
{
    readonly ConcurrentDictionary<string, VoiceBan> cache = new();
    readonly SemaphoreSlim writeLock = new(1, 1);

    public void Load()
    {
        using var db = dbs.CreateDbContext();
        foreach (var b in db.VoiceBans.AsNoTracking()) cache[b.Xuid] = b;
    }

    public VoiceBan? Active(string xuid) =>
        cache.TryGetValue(xuid, out var ban) && (ban.Until is null || ban.Until > DateTime.UtcNow) ? ban : null;

    public async Task Set(string xuid, DateTime? until)
    {
        await writeLock.WaitAsync();
        try
        {
            await using var db = await dbs.CreateDbContextAsync();
            var ban = new VoiceBan(xuid, until);
            if (cache.ContainsKey(xuid)) db.Update(ban); else db.Add(ban);
            await db.SaveChangesAsync();
            cache[xuid] = ban;
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task<bool> Remove(string xuid)
    {
        await writeLock.WaitAsync();
        try
        {
            if (!cache.TryRemove(xuid, out var ban)) return false;
            await using var db = await dbs.CreateDbContextAsync();
            db.Remove(ban);
            await db.SaveChangesAsync();
            return true;
        }
        finally
        {
            writeLock.Release();
        }
    }
}
