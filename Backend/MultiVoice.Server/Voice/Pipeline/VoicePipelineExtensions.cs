namespace MultiVoice.Server.Voice.Pipeline;

public static class VoicePipelineExtensions
{
    public static IServiceCollection AddVoicePipeline(this IServiceCollection services, Action<VoicePipelineBuilder> configure)
    {
        var builder = new VoicePipelineBuilder();
        configure(builder);
        return services.AddSingleton(builder.Build());
    }
}
