using Microsoft.EntityFrameworkCore;
using TTRPGHelper.Api.Infrastructure.Database;
using TTRPGHelper.Api.Modules.KnowledgeBase;
using TTRPGHelper.Api.Modules.KnowledgeBase.Entities;

namespace TTRPGHelper.Api.Modules.Conversations.Endpoints;

public static class ProposalEndpoints
{
    public static void MapProposalEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/proposals").WithTags("Proposals");

        group.MapGet("/", async (AppDbContext db, string? status, CancellationToken ct) =>
        {
            var query = db.NoteProposals.AsQueryable();
            if (status is not null)
                query = query.Where(p => p.Status == status);

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProposalListItem(
                    p.Id, p.ProposalType, p.TargetEntityName, p.NewEntityName,
                    p.NewEntityType, p.OriginalContent, p.ProposedContent,
                    p.Description, p.Status, p.CreatedAt))
                .ToListAsync(ct);
        });

        group.MapPost("/{id:guid}/approve", async (Guid id, ApproveProposalRequest? request, AppDbContext db, IngestionService ingestion, CancellationToken ct) =>
        {
            var proposal = await db.NoteProposals.FindAsync([id], ct);
            if (proposal is null) return Results.NotFound();
            if (proposal.Status != "pending") return Results.BadRequest("Proposal already resolved");

            var finalContent = request?.EditedContent ?? proposal.ProposedContent;

            if (proposal.ProposalType == "update" && proposal.TargetEntityId.HasValue)
            {
                var entity = await db.WorldEntities.FindAsync([proposal.TargetEntityId.Value], ct);
                if (entity is null) return Results.BadRequest("Target entity not found");

                entity.Content = finalContent;
                entity.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                await ingestion.IngestEntityAsync(entity, ct);
            }
            else if (proposal.ProposalType == "create")
            {
                var entity = new WorldEntity
                {
                    Id = Guid.NewGuid(),
                    Name = proposal.NewEntityName ?? "Untitled",
                    EntityType = proposal.NewEntityType ?? "Lore",
                    Description = proposal.Description,
                    Content = finalContent,
                    Tags = []
                };
                db.WorldEntities.Add(entity);
                await db.SaveChangesAsync(ct);
                await ingestion.IngestEntityAsync(entity, ct);
            }

            proposal.Status = "approved";
            proposal.ProposedContent = finalContent;
            proposal.ResolvedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Ok(proposal);
        });

        group.MapPost("/{id:guid}/reject", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var proposal = await db.NoteProposals.FindAsync([id], ct);
            if (proposal is null) return Results.NotFound();
            if (proposal.Status != "pending") return Results.BadRequest("Proposal already resolved");

            proposal.Status = "rejected";
            proposal.ResolvedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Ok(proposal);
        });

        group.MapPost("/bulk-approve", async (BulkResolveRequest request, AppDbContext db, IngestionService ingestion, CancellationToken ct) =>
        {
            var proposals = await db.NoteProposals
                .Where(p => request.Ids.Contains(p.Id) && p.Status == "pending")
                .ToListAsync(ct);

            foreach (var proposal in proposals)
            {
                if (proposal.ProposalType == "update" && proposal.TargetEntityId.HasValue)
                {
                    var entity = await db.WorldEntities.FindAsync([proposal.TargetEntityId.Value], ct);
                    if (entity is not null)
                    {
                        entity.Content = proposal.ProposedContent;
                        entity.UpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(ct);
                        await ingestion.IngestEntityAsync(entity, ct);
                    }
                }
                else if (proposal.ProposalType == "create")
                {
                    var entity = new WorldEntity
                    {
                        Id = Guid.NewGuid(),
                        Name = proposal.NewEntityName ?? "Untitled",
                        EntityType = proposal.NewEntityType ?? "Lore",
                        Description = proposal.Description,
                        Content = proposal.ProposedContent,
                        Tags = []
                    };
                    db.WorldEntities.Add(entity);
                    await db.SaveChangesAsync(ct);
                    await ingestion.IngestEntityAsync(entity, ct);
                }

                proposal.Status = "approved";
                proposal.ResolvedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { approved = proposals.Count });
        });

        group.MapPost("/bulk-reject", async (BulkResolveRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var proposals = await db.NoteProposals
                .Where(p => request.Ids.Contains(p.Id) && p.Status == "pending")
                .ToListAsync(ct);

            foreach (var proposal in proposals)
            {
                proposal.Status = "rejected";
                proposal.ResolvedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { rejected = proposals.Count });
        });
    }
}

public record ProposalListItem(
    Guid Id, string ProposalType, string? TargetEntityName, string? NewEntityName,
    string? NewEntityType, string? OriginalContent, string? ProposedContent,
    string? Description, string Status, DateTime CreatedAt);

public record ApproveProposalRequest(string? EditedContent);
public record BulkResolveRequest(List<Guid> Ids);
