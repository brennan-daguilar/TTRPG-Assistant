namespace TTRPGHelper.Api.Modules.Conversations.Entities;

public class NoteProposal
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public Guid? MessageId { get; set; }
    public ConversationMessage? Message { get; set; }

    public required string ProposalType { get; set; } // "update" or "create"
    public Guid? TargetEntityId { get; set; }
    public string? TargetEntityName { get; set; }

    // For create proposals
    public string? NewEntityName { get; set; }
    public string? NewEntityType { get; set; }

    // The proposed changes
    public string? OriginalContent { get; set; }
    public required string ProposedContent { get; set; }
    public string? Description { get; set; }

    public string Status { get; set; } = "pending"; // pending, approved, rejected
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
