using Microsoft.Extensions.AI;

namespace TTRPGHelper.Api.Infrastructure.AI.LLM;

public static class LlmServiceRegistration
{
    public static IServiceCollection AddLlmServices(this IServiceCollection services, IConfiguration configuration)
    {
        var llmConfig = configuration.GetSection(LlmProviderConfig.SectionName).Get<LlmProviderConfig>()
            ?? new LlmProviderConfig();
        var embeddingConfig = configuration.GetSection(EmbeddingProviderConfig.SectionName).Get<EmbeddingProviderConfig>()
            ?? new EmbeddingProviderConfig();

        services.AddSingleton(llmConfig);
        services.AddSingleton(embeddingConfig);

        services.AddChatClient(builder => CreateChatClient(llmConfig));
        services.AddEmbeddingGenerator(builder => CreateEmbeddingGenerator(embeddingConfig));

        return services;
    }

    private static IChatClient CreateChatClient(LlmProviderConfig config)
    {
        return config.Provider.ToLowerInvariant() switch
        {
            "ollama" => new OllamaChatClient(new Uri(config.Endpoint), config.Model),
            _ => throw new InvalidOperationException($"Unsupported LLM provider: {config.Provider}")
        };
    }

    private static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(EmbeddingProviderConfig config)
    {
        return config.Provider.ToLowerInvariant() switch
        {
            "ollama" => new OllamaEmbeddingGenerator(new Uri(config.Endpoint), config.Model),
            _ => throw new InvalidOperationException($"Unsupported embedding provider: {config.Provider}")
        };
    }
}
