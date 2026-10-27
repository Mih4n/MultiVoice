using Microsoft.Extensions.Options;
using MultiVoice.Server.Options;
using MultiVoice.Server.Voice.Pipeline;

namespace MultiVoice.Server.Voice;

public sealed class VoiceItems
{
    readonly int defaultRange;
    readonly Dictionary<string, VoiceItem> items;
    readonly Dictionary<string, Dictionary<string, float>> effects;

    public VoiceItems(IOptions<VoiceOptions> options)
    {
        var o = options.Value;
        defaultRange = o.DefaultRange;
        effects = o.Effects;
        items = o.Items.ToDictionary(i => i.Id);
        foreach (var (name, preset) in effects)
            if (preset.Keys.FirstOrDefault(k => !VoiceMix.Keys.ContainsKey(k)) is { } bad)
                throw new InvalidOperationException($"Voice:Effects:{name}: неизвестный ключ {bad}, есть {string.Join(", ", VoiceMix.Keys.Keys)}");
        foreach (var item in o.Items)
            if (item.Effects.FirstOrDefault(e => !effects.ContainsKey(e)) is { } missing)
                throw new InvalidOperationException($"Voice:Items {item.Id}: нет пресета {missing} в Voice:Effects");
        ChunkSize = o.Items.Select(i => i.Range).Append(defaultRange).Max();
    }

    public int ChunkSize { get; }

    public VoiceItem? Strongest(PlayerPosition p) => p.Items?
        .Select(id => items.GetValueOrDefault(id))
        .OfType<VoiceItem>()
        .MaxBy(i => i.Range);

    public int RangeOf(PlayerPosition p) => Strongest(p)?.Range ?? defaultRange;

    public void Apply(VoiceItem item, VoiceMix mix)
    {
        mix.Volume *= item.Volume;
        foreach (var name in item.Effects)
        foreach (var (key, value) in effects[name])
            VoiceMix.Keys[key](mix, value);
    }
}
