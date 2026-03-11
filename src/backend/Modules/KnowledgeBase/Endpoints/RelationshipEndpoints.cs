using Microsoft.EntityFrameworkCore;
using TTRPGHelper.Api.Infrastructure.Database;
using TTRPGHelper.Api.Modules.KnowledgeBase.Entities;

namespace TTRPGHelper.Api.Modules.KnowledgeBase.Endpoints;

public static class RelationshipEndpoints
{
    public static void MapRelationshipEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/relationships").WithTags("Relationships");

        group.MapGet("/entity/{entityId:guid}", async (Guid entityId, AppDbContext db, CancellationToken ct) =>
        {
            var from = await db.EntityRelationships
                .Where(r => r.FromEntityId == entityId)
                .Select(r => new RelationshipDto(r.Id, r.FromEntityId, r.FromEntity.Name, r.ToEntityId, r.ToEntity.Name, r.RelationshipType, r.Description))
                .ToListAsync(ct);

            var to = await db.EntityRelationships
                .Where(r => r.ToEntityId == entityId)
                .Select(r => new RelationshipDto(r.Id, r.FromEntityId, r.FromEntity.Name, r.ToEntityId, r.ToEntity.Name, r.RelationshipType, r.Description))
                .ToListAsync(ct);

            return from.Concat(to).ToList();
        });

        group.MapPost("/", async (CreateRelationshipRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var fromExists = await db.WorldEntities.AnyAsync(e => e.Id == request.FromEntityId, ct);
            var toExists = await db.WorldEntities.AnyAsync(e => e.Id == request.ToEntityId, ct);
            if (!fromExists || !toExists) return Results.BadRequest("One or both entities not found");

            var relationship = new EntityRelationship
            {
                Id = Guid.NewGuid(),
                FromEntityId = request.FromEntityId,
                ToEntityId = request.ToEntityId,
                RelationshipType = request.RelationshipType,
                Description = request.Description
            };

            db.EntityRelationships.Add(relationship);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/relationships/{relationship.Id}", relationship);
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var rel = await db.EntityRelationships.FindAsync([id], ct);
            if (rel is null) return Results.NotFound();

            db.EntityRelationships.Remove(rel);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });
    }
}

public record RelationshipDto(Guid Id, Guid FromEntityId, string FromEntityName, Guid ToEntityId, string ToEntityName, string RelationshipType, string? Description);
public record CreateRelationshipRequest(Guid FromEntityId, Guid ToEntityId, string RelationshipType, string? Description);
