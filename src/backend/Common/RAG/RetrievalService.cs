using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using TTRPGHelper.Api.Infrastructure.Database;
using TTRPGHelper.Api.Modules.KnowledgeBase.Entities;

namespace TTRPGHelper.Api.Common.RAG;

public class RetrievalService(AppDbContext db, EmbeddingService embeddingService)
{
    public async Task<List<RetrievalResult>> RetrieveAsync(
        string query,
        int topK = 10,
        string? entityTypeFilter = null,
        CancellationToken ct = default)
    {
        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query, ct);

        // Vector search
        var vectorQuery = db.EntityChunks
            .Include(c => c.WorldEntity)
            .Where(c => c.Embedding != null);

        if (entityTypeFilter is not null)
            vectorQuery = vectorQuery.Where(c => c.WorldEntity.EntityType == entityTypeFilter);

        var vectorResults = await vectorQuery
            .OrderBy(c => c.Embedding!.CosineDistance(queryEmbedding))
            .Take(topK)
            .Select(c => new RetrievalResult
            {
                ChunkId = c.Id,
                EntityId = c.WorldEntityId,
                EntityName = c.WorldEntity.Name,
                EntityType = c.WorldEntity.EntityType,
                Content = c.Content,
                SectionHeading = c.SectionHeading,
                Score = (float)(1 - c.Embedding!.CosineDistance(queryEmbedding))
            })
            .ToListAsync(ct);

        // Keyword augmentation: search entity names and tags
        var keywordResults = await db.WorldEntities
            .Where(e => EF.Functions.ILike(e.Name, $"%{query}%"))
            .SelectMany(e => e.Chunks)
            .Take(5)
            .Select(c => new RetrievalResult
            {
                ChunkId = c.Id,
                EntityId = c.WorldEntityId,
                EntityName = c.WorldEntity.Name,
                EntityType = c.WorldEntity.EntityType,
                Content = c.Content,
                SectionHeading = c.SectionHeading,
                Score = 0.8f // keyword matches get a fixed relevance boost
            })
            .ToListAsync(ct);

        // Merge and deduplicate
        var merged = vectorResults
            .Concat(keywordResults)
            .GroupBy(r => r.ChunkId)
            .Select(g => g.OrderByDescending(r => r.Score).First())
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        return merged;
    }
}

public class RetrievalResult
{
    public Guid ChunkId { get; init; }
    public Guid EntityId { get; init; }
    public required string EntityName { get; init; }
    public required string EntityType { get; init; }
    public required string Content { get; init; }
    public string? SectionHeading { get; init; }
    public float Score { get; init; }
}
