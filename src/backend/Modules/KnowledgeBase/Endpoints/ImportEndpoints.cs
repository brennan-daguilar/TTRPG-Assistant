using TTRPGHelper.Api.Infrastructure.Database;
using TTRPGHelper.Api.Modules.KnowledgeBase.Entities;

namespace TTRPGHelper.Api.Modules.KnowledgeBase.Endpoints;

public static class ImportEndpoints
{
    public static void MapImportEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/import").WithTags("Import");

        group.MapPost("/markdown", async (ImportMarkdownRequest request, AppDbContext db, IngestionService ingestion, CancellationToken ct) =>
        {
            var entity = new WorldEntity
            {
                Id = Guid.NewGuid(),
                Name = request.Name ?? ExtractNameFromContent(request.Content),
                EntityType = request.EntityType ?? "Lore",
                Description = request.Description,
                Content = request.Content,
                Tags = request.Tags ?? []
            };

            db.WorldEntities.Add(entity);
            await db.SaveChangesAsync(ct);

            await ingestion.IngestEntityAsync(entity, ct);

            return Results.Created($"/api/entities/{entity.Id}", entity);
        });

        group.MapPost("/bulk", async (IFormFileCollection files, string? entityType, AppDbContext db, IngestionService ingestion, CancellationToken ct) =>
        {
            var results = new List<BulkImportResult>();

            foreach (var file in files)
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var content = await reader.ReadToEndAsync(ct);
                var name = Path.GetFileNameWithoutExtension(file.FileName);

                var entity = new WorldEntity
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    EntityType = entityType ?? "Lore",
                    Content = content,
                    Tags = []
                };

                db.WorldEntities.Add(entity);
                await db.SaveChangesAsync(ct);

                try
                {
                    await ingestion.IngestEntityAsync(entity, ct);
                    results.Add(new BulkImportResult(name, true, null));
                }
                catch (Exception ex)
                {
                    results.Add(new BulkImportResult(name, false, ex.Message));
                }
            }

            return Results.Ok(results);
        }).DisableAntiforgery();
    }

    private static string ExtractNameFromContent(string content)
    {
        // Try to extract from first heading
        var firstLine = content.Split('\n').FirstOrDefault()?.Trim() ?? "Untitled";
        if (firstLine.StartsWith('#'))
            return firstLine.TrimStart('#', ' ');
        return firstLine.Length > 100 ? firstLine[..100] : firstLine;
    }
}

public record ImportMarkdownRequest(string Content, string? Name, string? EntityType, string? Description, List<string>? Tags);
public record BulkImportResult(string FileName, bool Success, string? Error);
