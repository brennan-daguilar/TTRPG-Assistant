using System.Text.RegularExpressions;
using TTRPGHelper.Api.Modules.Conversations.Entities;

namespace TTRPGHelper.Api.Modules.Conversations;

public static partial class ToolCallParser
{
    public static List<NoteProposal> ParseProposals(string content, Guid conversationId, Guid messageId)
    {
        var proposals = new List<NoteProposal>();

        // Parse UPDATE_NOTE blocks
        foreach (Match match in UpdateNoteRegex().Matches(content))
        {
            var entityName = match.Groups["entity"].Value.Trim();
            var description = match.Groups["description"].Value.Trim();
            var noteContent = match.Groups["content"].Value.Trim();

            if (!string.IsNullOrEmpty(entityName) && !string.IsNullOrEmpty(noteContent))
            {
                proposals.Add(new NoteProposal
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    MessageId = messageId,
                    ProposalType = "update",
                    TargetEntityName = entityName,
                    ProposedContent = noteContent,
                    Description = string.IsNullOrEmpty(description) ? null : description
                });
            }
        }

        // Parse CREATE_NOTE blocks
        foreach (Match match in CreateNoteRegex().Matches(content))
        {
            var name = match.Groups["name"].Value.Trim();
            var type = match.Groups["type"].Value.Trim();
            var description = match.Groups["description"].Value.Trim();
            var noteContent = match.Groups["content"].Value.Trim();

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(noteContent))
            {
                proposals.Add(new NoteProposal
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    MessageId = messageId,
                    ProposalType = "create",
                    NewEntityName = name,
                    NewEntityType = string.IsNullOrEmpty(type) ? "Lore" : type,
                    ProposedContent = noteContent,
                    Description = string.IsNullOrEmpty(description) ? null : description
                });
            }
        }

        return proposals;
    }

    [GeneratedRegex(
        @"<<<UPDATE_NOTE>>>\s*Entity:\s*(?<entity>.+?)\s*Description:\s*(?<description>.*?)\s*Content:\s*(?<content>.+?)<<<END_NOTE>>>",
        RegexOptions.Singleline)]
    private static partial Regex UpdateNoteRegex();

    [GeneratedRegex(
        @"<<<CREATE_NOTE>>>\s*Name:\s*(?<name>.+?)\s*Type:\s*(?<type>.*?)\s*Description:\s*(?<description>.*?)\s*Content:\s*(?<content>.+?)<<<END_NOTE>>>",
        RegexOptions.Singleline)]
    private static partial Regex CreateNoteRegex();
}
