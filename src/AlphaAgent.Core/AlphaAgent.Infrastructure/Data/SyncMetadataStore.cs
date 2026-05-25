using System;
using System.Threading.Tasks;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlphaAgent.Infrastructure.Data;

public class SyncMetadataStore : ISyncMetadataStore
{
    private readonly SharesDbContext _context;

    public SyncMetadataStore(SharesDbContext context)
    {
        _context = context;
    }

    public async Task<DateTime?> GetLastSyncTimeAsync(string key)
    {
        var entry = await _context.SyncMetadata.FindAsync(key);
        if (entry == null || string.IsNullOrEmpty(entry.Value))
            return null;

        if (DateTime.TryParse(entry.Value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            return dt;

        return null;
    }

    public async Task SetLastSyncTimeAsync(string key, DateTime time)
    {
        var isoString = time.ToUniversalTime().ToString("o");
        var entry = await _context.SyncMetadata.FindAsync(key);

        if (entry != null)
        {
            entry.Value = isoString;
        }
        else
        {
            _context.SyncMetadata.Add(new SyncMetadata { Key = key, Value = isoString });
        }

        await _context.SaveChangesAsync();
    }
}