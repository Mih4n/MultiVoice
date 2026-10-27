using Microsoft.EntityFrameworkCore;
using MultiVoice.Server.Auth;

namespace MultiVoice.Server.Settings;

public class SettingsDb(DbContextOptions<SettingsDb> options) : DbContext(options)
{
    public DbSet<SpeakerSetting> SpeakerSettings => Set<SpeakerSetting>();
    public DbSet<LoginToken> LoginTokens => Set<LoginToken>();
    public DbSet<VoiceBan> VoiceBans => Set<VoiceBan>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<SpeakerSetting>().HasKey(s => new { s.ListenerXuid, s.SpeakerXuid });
        model.Entity<LoginToken>().HasKey(t => t.Hash);
        model.Entity<VoiceBan>().HasKey(b => b.Xuid);
    }
}
