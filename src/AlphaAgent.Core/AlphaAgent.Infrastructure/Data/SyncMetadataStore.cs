using System;
using System.Threading.Tasks;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlphaAgent.Infrastructure.Data;

public class SyncMetadataStore : ISyncMetadataStore
{
    private readonly IDbContextFactory<SharesDbContext> _dbContextFactory;

    public SyncMetadataStore(IDbContextFactory<SharesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<DateTime?> GetLastSyncTimeAsync(string key)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var entry = await dbContext.SyncMetadata.FindAsync(key);
        if (entry == null || string.IsNullOrEmpty(entry.Value))
            return null;

        if (DateTime.TryParse(entry.Value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            return dt;

        return null;
    }

    public async Task SetLastSyncTimeAsync(string key, DateTime time)
    {
        var isoString = time.ToUniversalTime().ToString("o");
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var entry = await dbContext.SyncMetadata.FindAsync(key);

        if (entry != null)
        {
            entry.Value = isoString;
        }
        else
        {
            dbContext.SyncMetadata.Add(new SyncMetadata { Key = key, Value = isoString });
        }

        await dbContext.SaveChangesAsync();
    }
}