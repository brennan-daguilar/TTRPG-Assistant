using Microsoft.Extensions.AI;
using Pgvector;

namespace TTRPGHelper.Api.Common.RAG;

public class EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
{
    public async Task<Vector> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var embedding = await embeddingGenerator.GenerateAsync(text, cancellationToken: ct);
        return new Vector(embedding.Vector.ToArray());
    }

    public async Task<List<Vector>> GenerateEmbeddingsAsync(IList<string> texts, CancellationToken ct = default)
    {
        var embeddings = await embeddingGenerator.GenerateAsync(texts, cancellationToken: ct);
        return embeddings.Select(e => new Vector(e.Vector.ToArray())).ToList();
    }
}
