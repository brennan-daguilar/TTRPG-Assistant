using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using TTRPGHelper.Api.Common.RAG;

namespace TTRPGHelper.Api.Modules.Conversations;

public class ChatHub(IChatClient chatClient, RetrievalService retrievalService) : Hub
{
    public async IAsyncEnumerable<string> Ask(string question, Guid? conversationId, string? entityTypeFilter)
    {
        // Retrieve relevant context
        var results = await retrievalService.RetrieveAsync(question, entityTypeFilter: entityTypeFilter);

        // Build context from retrieved chunks
        var contextParts = results.Select(r =>
            $"[{r.EntityType}: {r.EntityName}]{(r.SectionHeading is not null ? $" ({r.SectionHeading})" : "")}\n{r.Content}");
        var context = string.Join("\n\n---\n\n", contextParts);

        var systemPrompt = $"""
            You are a TTRPG worldbuilding assistant. Answer questions based on the provided worldbuilding context.

            Rules:
            - Only use information from the provided context to answer questions
            - If the context doesn't contain enough information, say so clearly
            - Reference specific entities by name when citing information
            - Keep answers concise but thorough
            - If asked to update or create notes, describe what changes you would make

            Context:
            {context}
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, question)
        };

        await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
        {
            if (update.Text is not null)
                yield return update.Text;
        }

        // Send source references after streaming completes
        var sources = results.Select(r => new SourceReference(r.EntityId, r.EntityName, r.EntityType, r.SectionHeading, r.Score)).ToList();
        await Clients.Caller.SendAsync("SourceReferences", sources);
    }
}

public record SourceReference(Guid EntityId, string EntityName, string EntityType, string? SectionHeading, float Score);
