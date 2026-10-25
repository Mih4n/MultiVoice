using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MultiVoice.Server.Options;

namespace MultiVoice.Server.Minecraft;

public sealed class PluginSecret(IOptions<VoiceOptions> options)
{
    public bool Matches(string? given)
    {
        var secret = options.Value.McSecret;
        return !string.IsNullOrEmpty(secret) && given is not null &&
               CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(secret));
    }
}
