using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlphaAgent.Infrastructure.Data;

public class SharesDbContext : DbContext
{
    public DbSet<Security> Securities { get; set; } = null!;
    public DbSet<Quote> Quotes { get; set; } = null!;
    public DbSet<Token> Tokens { get; set; } = null!;
    public DbSet<AgentSession> AgentSessions { get; set; } = null!;
    public DbSet<AgentMessage> AgentMessages { get; set; } = null!;
    public DbSet<AgentTask> AgentTasks { get; set; } = null!;
    public DbSet<MessageCacheItem> MessageCache { get; set; } = null!;
    public DbSet<MomentCacheItem> MomentCaches { get; set; } = null!;
    public DbSet<VideoFeed> VideoFeeds { get; set; } = null!;
    public DbSet<ConversationCacheItem> ConversationCache { get; set; } = null!;
    public DbSet<ContactCacheItem> ContactCache { get; set; } = null!;
    public DbSet<SyncMetadata> SyncMetadata { get; set; } = null!;
    public DbSet<AgentConfigCacheItem> AgentConfigCache { get; set; } = null!;
    public DbSet<LlmConfigCacheItem> LlmConfigCache { get; set; } = null!;

    public SharesDbContext(DbContextOptions<SharesDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Security>().HasIndex(s => new { s.Code, s.Type }).IsUnique();
        modelBuilder.Entity<Quote>().HasIndex(q => new { q.SecurityId, q.Date, q.Freq }).IsUnique();
        
        modelBuilder.Entity<AgentSession>()
            .HasMany(s => s.Messages)
            .WithOne()
            .HasForeignKey(m => m.SessionId);
        
        // 添加索引提高查询性能
        modelBuilder.Entity<AgentSession>().HasIndex(s => new { s.UserId, s.Status });
        modelBuilder.Entity<AgentMessage>().HasIndex(m => m.SessionId);
        modelBuilder.Entity<AgentTask>().HasIndex(t => t.SessionId);
        
        modelBuilder.Entity<AgentMessage>()
            .OwnsMany(m => m.ToolCalls, tc =>
            {
                tc.Ignore(t => t.Input);
                tc.Ignore(t => t.Output);
            });

        modelBuilder.Entity<MomentCacheItem>().HasKey(m => m.Id);
        modelBuilder.Entity<MomentCacheItem>().HasIndex(m => m.CreatedAt);
        modelBuilder.Entity<MomentCacheItem>().HasIndex(m => m.TargetId);

        modelBuilder.Entity<VideoFeed>().HasKey(v => v.Id);
        modelBuilder.Entity<VideoFeed>().HasIndex(v => v.CreatedAt);

        modelBuilder.Entity<MessageCacheItem>().HasKey(m => m.Id);
        modelBuilder.Entity<MessageCacheItem>().HasIndex(m => m.ConversationId);

        modelBuilder.Entity<ConversationCacheItem>().HasKey(c => c.Id);
        modelBuilder.Entity<ConversationCacheItem>().HasIndex(c => c.UserId);
        modelBuilder.Entity<ConversationCacheItem>().HasIndex(c => new { c.UserId, c.LastMessageTime });

        modelBuilder.Entity<ContactCacheItem>().HasKey(c => c.Id);
        modelBuilder.Entity<ContactCacheItem>().HasIndex(c => c.UserId);
        modelBuilder.Entity<ContactCacheItem>().HasIndex(c => new { c.UserId, c.Type });

        modelBuilder.Entity<SyncMetadata>().HasKey(s => s.Key);

        modelBuilder.Entity<AgentConfigCacheItem>().HasKey(a => a.Id);
        modelBuilder.Entity<AgentConfigCacheItem>().HasIndex(a => a.UserId);
        modelBuilder.Entity<AgentConfigCacheItem>().HasIndex(a => new { a.UserId, a.AgentName, a.IsActive });
        modelBuilder.Entity<AgentConfigCacheItem>().Ignore(a => a.EnabledTools);

        modelBuilder.Entity<LlmConfigCacheItem>().HasKey(l => l.Id);
        modelBuilder.Entity<LlmConfigCacheItem>().HasIndex(l => l.UserId);
    }
}