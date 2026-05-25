using AlphaAgent.Domain.Abstractions;
using AlphaAgent.Infrastructure.Data;
using AlphaAgent.Infrastructure.InitData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Services.Database;

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly SharesDbContext _dbContext;

    public DatabaseInitializer(SharesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
        await EnsureNewTablesAsync();
        await EnsureNewColumnsAsync();
        await SeedInitialDataAsync();
    }

    /// <summary>
    /// EnsureCreatedAsync 不会给已存在的数据库添加新表，需手动建表
    /// </summary>
    private async Task EnsureNewTablesAsync()
    {
        var sql = """
            CREATE TABLE IF NOT EXISTS "ConversationCache" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ConversationCache" PRIMARY KEY,
                "Type" INTEGER NOT NULL,
                "Name" TEXT,
                "OtherUserName" TEXT,
                "OtherUserId" TEXT,
                "OtherDeviceId" TEXT,
                "DeviceType" TEXT,
                "UnreadCount" INTEGER NOT NULL,
                "LastMessage" TEXT,
                "LastMessageTime" TEXT,
                "MemberCount" INTEGER NOT NULL,
                "Context" TEXT,
                "CachedAt" TEXT NOT NULL,
                "UserId" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_ConversationCache_UserId" ON "ConversationCache" ("UserId");
            CREATE INDEX IF NOT EXISTS "IX_ConversationCache_UserId_LastMessageTime" ON "ConversationCache" ("UserId", "LastMessageTime");

            CREATE TABLE IF NOT EXISTS "ContactCache" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ContactCache" PRIMARY KEY,
                "Type" INTEGER NOT NULL,
                "TargetId" TEXT NOT NULL,
                "TargetName" TEXT NOT NULL,
                "DeviceType" TEXT,
                "Status" INTEGER NOT NULL,
                "CachedAt" TEXT NOT NULL,
                "UserId" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_ContactCache_UserId" ON "ContactCache" ("UserId");
            CREATE INDEX IF NOT EXISTS "IX_ContactCache_UserId_Type" ON "ContactCache" ("UserId", "Type");

            CREATE TABLE IF NOT EXISTS "SyncMetadata" (
                "Key" TEXT NOT NULL CONSTRAINT "PK_SyncMetadata" PRIMARY KEY,
                "Value" TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "AgentConfigCacheItem" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_AgentConfigCacheItem" PRIMARY KEY,
                "UserId" TEXT NOT NULL,
                "AgentName" TEXT NOT NULL,
                "ModelName" TEXT NOT NULL,
                "ApiKey" TEXT NOT NULL,
                "Endpoint" TEXT NOT NULL,
                "DefaultSystemPrompt" TEXT NOT NULL,
                "Temperature" REAL NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "CachedAt" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_AgentConfigCacheItem_UserId" ON "AgentConfigCacheItem" ("UserId");
            CREATE INDEX IF NOT EXISTS "IX_AgentConfigCacheItem_UserId_AgentName_IsActive" ON "AgentConfigCacheItem" ("UserId", "AgentName", "IsActive");
            """;
        await _dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    /// <summary>
    /// EnsureCreatedAsync 不会给已存在的表添加新列，需手动迁移
    /// </summary>
    private async Task EnsureNewColumnsAsync()
    {
        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            using var cmd = connection.CreateCommand();

            // Securities.UpdatedAt
            cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Securities') WHERE name='UpdatedAt'";
            var result = (long?)await cmd.ExecuteScalarAsync();
            if (result == 0)
            {
                cmd.CommandText = "ALTER TABLE Securities ADD COLUMN \"UpdatedAt\" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000Z'";
                await cmd.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private async Task SeedInitialDataAsync()
    {
        // Securities 不再从静态文件 seed，改为从服务端同步
        // 保留 VideoFeeds 的静态 seed
        if (!await _dbContext.VideoFeeds.AnyAsync())
        {
            _dbContext.VideoFeeds.AddRange(VideoFeedData.All);
            await _dbContext.SaveChangesAsync();
        }
    }
}
