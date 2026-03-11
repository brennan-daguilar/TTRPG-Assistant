namespace TTRPGHelper.Api.Infrastructure.AI.LLM;

public class LlmProviderConfig
{
    public const string SectionName = "LlmProvider";

    public string Provider { get; set; } = "ollama";
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3";
    public string? ApiKey { get; set; }
}

public class EmbeddingProviderConfig
{
    public const string SectionName = "EmbeddingProvider";

    public string Provider { get; set; } = "ollama";
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "nomic-embed-text";
    public string? ApiKey { get; set; }
    public int Dimensions { get; set; } = 768;
}
