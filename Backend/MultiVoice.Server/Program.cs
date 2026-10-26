using Microsoft.EntityFrameworkCore;
using MultiVoice.Server.Auth;
using MultiVoice.Server.Dev;
using MultiVoice.Server.Minecraft;
using MultiVoice.Server.Options;
using MultiVoice.Server.Players;
using MultiVoice.Server.Settings;
using MultiVoice.Server.Voice;
using MultiVoice.Server.Voice.Effects;
using MultiVoice.Server.Voice.Pipeline;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var voiceSection = builder.Configuration.GetSection(VoiceOptions.Section);
var voiceOptions = voiceSection.Get<VoiceOptions>() ?? new VoiceOptions();
services.Configure<VoiceOptions>(voiceSection);

services.AddSignalR().AddMessagePackProtocol();
services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(voiceOptions.Origins)
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

services.AddSingleton<PlayerRegistry>();
services.AddSingleton<PluginSecret>();
services.AddSingleton<MinecraftLink>();
services.AddSingleton<LoginService>();
services.AddSingleton<VoiceSessions>();
services.AddSingleton<VoiceRouter>();
services.AddSingleton<VoiceStates>();
services.AddDbContextFactory<SettingsDb>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Settings")));
services.AddSingleton<SpeakerSettings>();

services.AddVoicePipeline(p => p
    .Use(new MutedSpeaker())
    .Use(new DistanceFade(from: 4, to: voiceOptions.ChunkSize))
    .Use(new UnderwaterMuffle(cutoff: 600))
    .Use(new CaveReverb(wet: 0.5f, decay: 2.5f))
    .Use(new DeepCaveEcho(belowY: 0, delay: 0.3f, feedback: 0.35f, wet: 0.3f)));

var app = builder.Build();
app.Services.GetRequiredService<SpeakerSettings>().Load();
app.UseCors();
app.MapHub<AuthHub>("/api/auth");
app.MapHub<VoiceHub>("/api/voice");
app.MapHub<MinecraftHub>("/api/minecraft");
if (voiceOptions.DevHub) app.MapHub<DevHub>("/api/dev");
app.Run();
