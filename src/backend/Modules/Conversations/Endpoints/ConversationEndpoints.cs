using Microsoft.EntityFrameworkCore;
using TTRPGHelper.Api.Infrastructure.Database;
using TTRPGHelper.Api.Modules.Conversations.Entities;

namespace TTRPGHelper.Api.Modules.Conversations.Endpoints;

public static class ConversationEndpoints
{
    public static void MapConversationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/conversations").WithTags("Conversations");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
        {
            return await db.Conversations
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new ConversationListItem(c.Id, c.Title, c.CreatedAt, c.UpdatedAt))
                .ToListAsync(ct);
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var conversation = await db.Conversations
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            return conversation is null ? Results.NotFound() : Results.Ok(conversation);
        });

        group.MapPost("/", async (CreateConversationRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Title = request.Title ?? "New Conversation"
            };

            db.Conversations.Add(conversation);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/conversations/{conversation.Id}", conversation);
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var conversation = await db.Conversations.FindAsync([id], ct);
            if (conversation is null) return Results.NotFound();

            db.Conversations.Remove(conversation);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });
    }
}

public record ConversationListItem(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);
public record CreateConversationRequest(string? Title);
