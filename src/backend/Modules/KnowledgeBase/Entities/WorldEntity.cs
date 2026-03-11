using Pgvector;

namespace TTRPGHelper.Api.Modules.KnowledgeBase.Entities;

public class WorldEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string EntityType { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public List<string> Tags { get; set; } = [];
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<EntityChunk> Chunks { get; set; } = [];
    public List<EntityRelationship> RelationshipsFrom { get; set; } = [];
    public List<EntityRelationship> RelationshipsTo { get; set; } = [];
}

public class EntityChunk
{
    public Guid Id { get; set; }
    public Guid WorldEntityId { get; set; }
    public WorldEntity WorldEntity { get; set; } = null!;
    public required string Content { get; set; }
    public string? SectionHeading { get; set; }
    public int ChunkIndex { get; set; }
    public string? ContentHash { get; set; }
    public string? EmbeddingModel { get; set; }
    public Vector? Embedding { get; set; }
    public List<string> ReferencedEntityNames { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EntityRelationship
{
    public Guid Id { get; set; }
    public Guid FromEntityId { get; set; }
    public WorldEntity FromEntity { get; set; } = null!;
    public Guid ToEntityId { get; set; }
    public WorldEntity ToEntity { get; set; } = null!;
    public required string RelationshipType { get; set; }
    public string? Description { get; set; }
}
