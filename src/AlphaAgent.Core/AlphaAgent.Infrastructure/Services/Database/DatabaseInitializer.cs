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

        await SeedInitialDataAsync();
    }

    
    /// <summary>
    /// EnsureCreatedAsync 不会给已存在的表添加新列，需手动迁移
    /// </summary>


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
