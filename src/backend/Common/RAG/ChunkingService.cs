using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace TTRPGHelper.Api.Common.RAG;

public partial class ChunkingService
{
    private const int TargetChunkTokens = 400;
    private const int MaxChunkTokens = 500;
    private const int MinChunkTokens = 50;

    public List<ChunkResult> ChunkContent(string content, string entityName)
    {
        var sections = SplitIntoSections(content);
        var chunks = new List<ChunkResult>();
        var chunkIndex = 0;

        foreach (var section in sections)
        {
            var sectionChunks = SplitSectionIntoChunks(section.Content);
            foreach (var chunkContent in sectionChunks)
            {
                var trimmed = chunkContent.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || EstimateTokens(trimmed) < MinChunkTokens)
                    continue;

                chunks.Add(new ChunkResult
                {
                    Content = trimmed,
                    SectionHeading = section.Heading,
                    ChunkIndex = chunkIndex++,
                    ContentHash = ComputeHash(trimmed),
                    ReferencedEntityNames = ExtractEntityReferences(trimmed, entityName)
                });
            }
        }

        return chunks;
    }

    private static List<Section> SplitIntoSections(string content)
    {
        var sections = new List<Section>();
        var lines = content.Split('\n');
        string? currentHeading = null;
        var currentContent = new StringBuilder();

        foreach (var line in lines)
        {
            if (HeadingRegex().IsMatch(line))
            {
                if (currentContent.Length > 0)
                {
                    sections.Add(new Section(currentHeading, currentContent.ToString()));
                    currentContent.Clear();
                }
                currentHeading = line.TrimStart('#', ' ');
            }
            else
            {
                currentContent.AppendLine(line);
            }
        }

        if (currentContent.Length > 0)
            sections.Add(new Section(currentHeading, currentContent.ToString()));

        return sections;
    }

    private static List<string> SplitSectionIntoChunks(string content)
    {
        var estimatedTokens = EstimateTokens(content);
        if (estimatedTokens <= MaxChunkTokens)
            return [content];

        var paragraphs = content.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        var currentChunk = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (EstimateTokens(currentChunk.ToString() + paragraph) > MaxChunkTokens && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString());
                currentChunk.Clear();
            }
            currentChunk.AppendLine(paragraph);
            currentChunk.AppendLine();
        }

        if (currentChunk.Length > 0)
            chunks.Add(currentChunk.ToString());

        return chunks;
    }

    private static int EstimateTokens(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length * 4 / 3;

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }

    private static List<string> ExtractEntityReferences(string content, string currentEntityName)
    {
        // Basic extraction: find capitalized multi-word names that could be entity references
        // This will be enhanced later with actual entity name matching
        var references = new List<string>();
        var matches = ProperNounRegex().Matches(content);
        foreach (Match match in matches)
        {
            var name = match.Value.Trim();
            if (name != currentEntityName && name.Length > 2)
                references.Add(name);
        }
        return references.Distinct().ToList();
    }

    [GeneratedRegex(@"^#{1,6}\s+", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)+\b")]
    private static partial Regex ProperNounRegex();
}

public record Section(string? Heading, string Content);

public record ChunkResult
{
    public required string Content { get; init; }
    public string? SectionHeading { get; init; }
    public int ChunkIndex { get; init; }
    public required string ContentHash { get; init; }
    public List<string> ReferencedEntityNames { get; init; } = [];
}
