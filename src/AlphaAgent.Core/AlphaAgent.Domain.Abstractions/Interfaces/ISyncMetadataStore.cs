using System;
using System.Threading.Tasks;

namespace AlphaAgent.Domain.Abstractions.Interfaces;

public interface ISyncMetadataStore
{
    Task<DateTime?> GetLastSyncTimeAsync(string key);
    Task SetLastSyncTimeAsync(string key, DateTime time);
}