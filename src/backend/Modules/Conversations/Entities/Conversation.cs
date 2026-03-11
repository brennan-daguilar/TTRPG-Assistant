namespace TTRPGHelper.Api.Modules.Conversations.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ConversationMessage> Messages { get; set; } = [];
}

public class ConversationMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public required string Role { get; set; }
    public required string Content { get; set; }
    public List<Guid>? ReferencedChunkIds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
