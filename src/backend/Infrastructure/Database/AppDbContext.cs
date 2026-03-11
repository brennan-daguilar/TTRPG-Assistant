using Microsoft.EntityFrameworkCore;
using TTRPGHelper.Api.Modules.KnowledgeBase.Entities;
using TTRPGHelper.Api.Modules.Conversations.Entities;

namespace TTRPGHelper.Api.Infrastructure.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WorldEntity> WorldEntities => Set<WorldEntity>();
    public DbSet<EntityChunk> EntityChunks => Set<EntityChunk>();
    public DbSet<EntityRelationship> EntityRelationships => Set<EntityRelationship>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<WorldEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.EntityType);
            entity.Property(e => e.Tags).HasColumnType("text[]");
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
        });

        modelBuilder.Entity<EntityChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WorldEntityId);
            entity.Property(e => e.Embedding).HasColumnType("vector(768)");
            entity.Property(e => e.ReferencedEntityNames).HasColumnType("text[]");
            entity.HasOne(e => e.WorldEntity)
                .WithMany(w => w.Chunks)
                .HasForeignKey(e => e.WorldEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");
        });

        modelBuilder.Entity<EntityRelationship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.FromEntity)
                .WithMany(w => w.RelationshipsFrom)
                .HasForeignKey(e => e.FromEntityId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ToEntity)
                .WithMany(w => w.RelationshipsTo)
                .HasForeignKey(e => e.ToEntityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConversationId);
            entity.Property(e => e.ReferencedChunkIds).HasColumnType("uuid[]");
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
