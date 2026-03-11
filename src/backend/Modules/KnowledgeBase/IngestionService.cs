using Microsoft.EntityFrameworkCore;
using TTRPGHelper.Api.Common.RAG;
using TTRPGHelper.Api.Infrastructure.Database;
using TTRPGHelper.Api.Modules.KnowledgeBase.Entities;

namespace TTRPGHelper.Api.Modules.KnowledgeBase;

public class IngestionService(AppDbContext db, ChunkingService chunkingService, EmbeddingService embeddingService)
{
    public async Task IngestEntityAsync(WorldEntity entity, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entity.Content))
            return;

        var chunkResults = chunkingService.ChunkContent(entity.Content, entity.Name);

        // Check for existing chunks that haven't changed (hash comparison)
        var existingChunks = await db.EntityChunks
            .Where(c => c.WorldEntityId == entity.Id)
            .ToListAsync(ct);

        var existingHashes = existingChunks.ToDictionary(c => c.ContentHash ?? "", c => c);
        var newChunks = new List<EntityChunk>();
        var unchangedIds = new HashSet<Guid>();

        foreach (var result in chunkResults)
        {
            if (existingHashes.TryGetValue(result.ContentHash, out var existing))
            {
                // Chunk content hasn't changed, update metadata only
                existing.ChunkIndex = result.ChunkIndex;
                existing.SectionHeading = result.SectionHeading;
                existing.ReferencedEntityNames = result.ReferencedEntityNames;
                unchangedIds.Add(existing.Id);
            }
            else
            {
                newChunks.Add(new EntityChunk
                {
                    Id = Guid.NewGuid(),
                    WorldEntityId = entity.Id,
                    Content = result.Content,
                    SectionHeading = result.SectionHeading,
                    ChunkIndex = result.ChunkIndex,
                    ContentHash = result.ContentHash,
                    ReferencedEntityNames = result.ReferencedEntityNames
                });
            }
        }

        // Remove chunks that no longer exist
        var toRemove = existingChunks.Where(c => !unchangedIds.Contains(c.Id)).ToList();
        db.EntityChunks.RemoveRange(toRemove);

        // Generate embeddings for new chunks
        if (newChunks.Count > 0)
        {
            var texts = newChunks.Select(c => c.Content).ToList();
            var embeddings = await embeddingService.GenerateEmbeddingsAsync(texts, ct);

            for (var i = 0; i < newChunks.Count; i++)
            {
                newChunks[i].Embedding = embeddings[i];
                newChunks[i].EmbeddingModel = "nomic-embed-text"; // TODO: from config
            }

            await db.EntityChunks.AddRangeAsync(newChunks, ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
