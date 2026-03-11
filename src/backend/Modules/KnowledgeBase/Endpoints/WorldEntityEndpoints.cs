using Microsoft.EntityFrameworkCore;
using TTRPGHelper.Api.Infrastructure.Database;
using TTRPGHelper.Api.Modules.KnowledgeBase.Entities;

namespace TTRPGHelper.Api.Modules.KnowledgeBase.Endpoints;

public static class WorldEntityEndpoints
{
    public static void MapWorldEntityEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/entities").WithTags("Entities");

        group.MapGet("/", async (AppDbContext db, string? type, string? search, CancellationToken ct) =>
        {
            var query = db.WorldEntities.AsQueryable();

            if (type is not null)
                query = query.Where(e => e.EntityType == type);

            if (search is not null)
                query = query.Where(e => EF.Functions.ILike(e.Name, $"%{search}%"));

            return await query
                .OrderBy(e => e.Name)
                .Select(e => new EntityListItem(e.Id, e.Name, e.EntityType, e.Tags, e.Description, e.UpdatedAt))
                .ToListAsync(ct);
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var entity = await db.WorldEntities
                .Include(e => e.RelationshipsFrom).ThenInclude(r => r.ToEntity)
                .Include(e => e.RelationshipsTo).ThenInclude(r => r.FromEntity)
                .FirstOrDefaultAsync(e => e.Id == id, ct);

            return entity is null ? Results.NotFound() : Results.Ok(entity);
        });

        group.MapPost("/", async (CreateEntityRequest request, AppDbContext db, IngestionService ingestion, CancellationToken ct) =>
        {
            var entity = new WorldEntity
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                EntityType = request.EntityType,
                Description = request.Description,
                Content = request.Content,
                Tags = request.Tags ?? []
            };

            db.WorldEntities.Add(entity);
            await db.SaveChangesAsync(ct);

            await ingestion.IngestEntityAsync(entity, ct);

            return Results.Created($"/api/entities/{entity.Id}", entity);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateEntityRequest request, AppDbContext db, IngestionService ingestion, CancellationToken ct) =>
        {
            var entity = await db.WorldEntities.FindAsync([id], ct);
            if (entity is null) return Results.NotFound();

            entity.Name = request.Name ?? entity.Name;
            entity.EntityType = request.EntityType ?? entity.EntityType;
            entity.Description = request.Description ?? entity.Description;
            entity.Content = request.Content ?? entity.Content;
            if (request.Tags is not null) entity.Tags = request.Tags;
            entity.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            await ingestion.IngestEntityAsync(entity, ct);

            return Results.Ok(entity);
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var entity = await db.WorldEntities.FindAsync([id], ct);
            if (entity is null) return Results.NotFound();

            db.WorldEntities.Remove(entity);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        group.MapGet("/types", async (AppDbContext db, CancellationToken ct) =>
        {
            return await db.WorldEntities
                .Select(e => e.EntityType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync(ct);
        });
    }
}

public record EntityListItem(Guid Id, string Name, string EntityType, List<string> Tags, string? Description, DateTime UpdatedAt);
public record CreateEntityRequest(string Name, string EntityType, string? Description, string? Content, List<string>? Tags);
public record UpdateEntityRequest(string? Name, string? EntityType, string? Description, string? Content, List<string>? Tags);
