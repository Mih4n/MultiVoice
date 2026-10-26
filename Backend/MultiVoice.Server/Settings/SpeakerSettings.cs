using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MultiVoice.Server.Settings;

public sealed class SpeakerSettings(IDbContextFactory<SettingsDb> dbs)
{
    public const float MaxVolume = 5;

    readonly ConcurrentDictionary<(string Listener, string Speaker), SpeakerSetting> cache = new();
    readonly SemaphoreSlim writeLock = new(1, 1);

    public void Load()
    {
        using var db = dbs.CreateDbContext();
        var file = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        if (Path.GetDirectoryName(Path.GetFullPath(file)) is { } dir) Directory.CreateDirectory(dir);
        db.Database.Migrate();
        foreach (var s in db.SpeakerSettings.AsNoTracking()) cache[(s.ListenerXuid, s.SpeakerXuid)] = s;
    }

    public SpeakerSetting? Get(string listener, string speaker) => cache.GetValueOrDefault((listener, speaker));

    public Dictionary<string, SpeakerSetting> Of(string listener) =>
        cache.Values.Where(s => s.ListenerXuid == listener).ToDictionary(s => s.SpeakerXuid);

    public async Task Change(string listener, string speaker, Func<SpeakerSetting, SpeakerSetting> change)
    {
        await writeLock.WaitAsync();
        try
        {
            var old = Get(listener, speaker);
            var next = change(old ?? new SpeakerSetting(listener, speaker, 1, false));
            await using var db = await dbs.CreateDbContextAsync();
            if (old is null) db.Add(next); else db.Update(next);
            await db.SaveChangesAsync();
            cache[(listener, speaker)] = next;
        }
        finally
        {
            writeLock.Release();
        }
    }
}
