using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using TTRPGHelper.Api.Common.RAG;
using TTRPGHelper.Api.Infrastructure.Database;
using TTRPGHelper.Api.Modules.Conversations.Entities;

namespace TTRPGHelper.Api.Modules.Conversations;

public class ChatHub(
    IChatClient chatClient,
    RetrievalService retrievalService,
    AppDbContext db) : Hub
{
    private const int MaxHistoryMessages = 20;

    public async IAsyncEnumerable<string> Ask(string question, Guid? conversationId, string? entityTypeFilter)
    {
        // Create or load conversation
        Conversation conversation;
        if (conversationId.HasValue)
        {
            conversation = await db.Conversations
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(c => c.Id == conversationId.Value)
                ?? throw new HubException("Conversation not found");
        }
        else
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Title = question.Length > 80 ? question[..80] + "..." : question
            };
            db.Conversations.Add(conversation);
            await db.SaveChangesAsync();

            // Notify client of the new conversation ID
            await Clients.Caller.SendAsync("ConversationCreated", conversation.Id, conversation.Title);
        }

        // Retrieve relevant context
        var results = await retrievalService.RetrieveAsync(question, entityTypeFilter: entityTypeFilter);

        var contextParts = results.Select(r =>
            $"[{r.EntityType}: {r.EntityName}]{(r.SectionHeading is not null ? $" ({r.SectionHeading})" : "")}\n{r.Content}");
        var context = string.Join("\n\n---\n\n", contextParts);

        var systemPrompt = BuildSystemPrompt(context);

        // Build message history
        var chatMessages = new List<ChatMessage> { new(ChatRole.System, systemPrompt) };

        // Add recent conversation history (limit to avoid context overflow)
        var recentMessages = conversation.Messages
            .OrderByDescending(m => m.CreatedAt)
            .Take(MaxHistoryMessages)
            .Reverse()
            .ToList();

        foreach (var msg in recentMessages)
        {
            chatMessages.Add(new ChatMessage(
                msg.Role == "user" ? ChatRole.User : ChatRole.Assistant,
                msg.Content));
        }

        // Add current question
        chatMessages.Add(new ChatMessage(ChatRole.User, question));

        // Persist user message
        var userMsg = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "user",
            Content = question,
            ReferencedChunkIds = results.Select(r => r.ChunkId).ToList()
        };
        db.ConversationMessages.Add(userMsg);
        conversation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Stream response
        var responseBuffer = new System.Text.StringBuilder();

        await foreach (var update in chatClient.GetStreamingResponseAsync(chatMessages))
        {
            if (update.Text is not null)
            {
                responseBuffer.Append(update.Text);
                yield return update.Text;
            }
        }

        // Persist assistant message
        var assistantContent = responseBuffer.ToString();
        var assistantMsg = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = assistantContent
        };
        db.ConversationMessages.Add(assistantMsg);
        await db.SaveChangesAsync();

        // Parse tool calls from the response and create proposals
        var proposals = ToolCallParser.ParseProposals(assistantContent, conversation.Id, assistantMsg.Id);
        if (proposals.Count > 0)
        {
            // Resolve entity IDs for update proposals
            foreach (var proposal in proposals)
            {
                if (proposal.ProposalType == "update" && proposal.TargetEntityName is not null)
                {
                    var entity = await db.WorldEntities
                        .FirstOrDefaultAsync(e => EF.Functions.ILike(e.Name, proposal.TargetEntityName));
                    if (entity is not null)
                    {
                        proposal.TargetEntityId = entity.Id;
                        proposal.OriginalContent = entity.Content;
                    }
                }
            }

            db.NoteProposals.AddRange(proposals);
            await db.SaveChangesAsync();

            await Clients.Caller.SendAsync("NoteProposals", proposals.Select(p => new NoteProposalDto(
                p.Id, p.ProposalType, p.TargetEntityName, p.NewEntityName, p.NewEntityType,
                p.OriginalContent, p.ProposedContent, p.Description, p.Status)).ToList());
        }

        // Send source references
        var sources = results.Select(r => new SourceReference(r.EntityId, r.EntityName, r.EntityType, r.SectionHeading, r.Score)).ToList();
        await Clients.Caller.SendAsync("SourceReferences", sources);
    }

    private static string BuildSystemPrompt(string context)
    {
        return $"""
            You are a TTRPG worldbuilding assistant. Answer questions based on the provided worldbuilding context.

            Rules:
            - Only use information from the provided context to answer questions
            - If the context doesn't contain enough information, say so clearly
            - Reference specific entities by name when citing information
            - Keep answers concise but thorough

            When the user asks you to update or create notes, include a structured block in your response using this exact format:

            <<<UPDATE_NOTE>>>
            Entity: [exact entity name]
            Description: [brief description of the change]
            Content:
            [the full updated content for the entity]
            <<<END_NOTE>>>

            For creating new entities:

            <<<CREATE_NOTE>>>
            Name: [entity name]
            Type: [Character|Location|Faction|Item|Event|Lore]
            Description: [brief description]
            Content:
            [the content for the new entity]
            <<<END_NOTE>>>

            Context:
            {context}
            """;
    }
}

public record SourceReference(Guid EntityId, string EntityName, string EntityType, string? SectionHeading, float Score);

public record NoteProposalDto(
    Guid Id, string ProposalType, string? TargetEntityName, string? NewEntityName,
    string? NewEntityType, string? OriginalContent, string? ProposedContent,
    string? Description, string Status);
