using AlphaAgent.Domain.Abstractions;
using AlphaAgent.Infrastructure.Data;
using AlphaAgent.Infrastructure.InitData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Services.Database;

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<SharesDbContext> _dbContextFactory;

    public DatabaseInitializer(IDbContextFactory<SharesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task InitializeAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.EnsureCreatedAsync();

        await SeedInitialDataAsync(dbContext);
    }

    private async Task SeedInitialDataAsync(SharesDbContext dbContext)
    {
        // Securities 不再从静态文件 seed，改为从服务端同步
        // 保留 VideoFeeds 的静态 seed
        if (!await dbContext.VideoFeeds.AnyAsync())
        {
            dbContext.VideoFeeds.AddRange(VideoFeedData.All);
            await dbContext.SaveChangesAsync();
        }
    }
}