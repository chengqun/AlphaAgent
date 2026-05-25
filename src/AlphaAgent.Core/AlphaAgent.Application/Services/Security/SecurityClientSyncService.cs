using System;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Application.Dtos.Security;
using AlphaAgent.Application.Interfaces.Security;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Interfaces;

namespace AlphaAgent.Application.Services.Security;

public class SecurityClientSyncService : ISecurityClientSyncService
{
    private const string SyncTimeKey = "SecurityLastSyncTime";
    private const string SyncEndpoint = "api/app/security-client-sync/updates";

    private readonly ISecurityRepository _securityRepository;
    private readonly IHttpClientService _httpClientService;
    private readonly ISyncMetadataStore _syncMetadataStore;

    public SecurityClientSyncService(
        ISecurityRepository securityRepository,
        IHttpClientService httpClientService,
        ISyncMetadataStore syncMetadataStore)
    {
        _securityRepository = securityRepository;
        _httpClientService = httpClientService;
        _syncMetadataStore = syncMetadataStore;
    }

    public async Task<SecuritySyncResult> SyncFromServerAsync()
    {
        var lastSyncTime = await _syncMetadataStore.GetLastSyncTimeAsync(SyncTimeKey);
        Console.WriteLine($"[SecuritySync] lastSyncTime={lastSyncTime}");

        try
        {
            var endpoint = lastSyncTime.HasValue
                ? $"{SyncEndpoint}?after={Uri.EscapeDataString(lastSyncTime.Value.ToString("o"))}"
                : SyncEndpoint;

            Console.WriteLine($"[SecuritySync] GET {endpoint}");
            var result = await _httpClientService.GetAsync<SecuritySyncResponseDto>(endpoint);

            if (result == null)
            {
                Console.WriteLine("[SecuritySync] No data returned");
                return new SecuritySyncResult { LastSyncTime = lastSyncTime };
            }

            if (result.Securities == null || result.Securities.Count == 0)
            {
                Console.WriteLine("[SecuritySync] Empty securities list");
                return new SecuritySyncResult { LastSyncTime = lastSyncTime };
            }

            Console.WriteLine($"[SecuritySync] Received {result.Securities.Count} securities, IsFullSync={result.IsFullSync}, ServerTime={result.ServerTime}");

            var securities = result.Securities.Select(dto =>
                new Domain.Entities.Security(0, dto.Code, dto.Name, dto.Type, dto.Exchange, dto.BaseCode, dto.UpdatedAt)).ToList();

            await _securityRepository.AddRangeAsync(securities, 1000);

            await _syncMetadataStore.SetLastSyncTimeAsync(SyncTimeKey, result.ServerTime);

            Console.WriteLine($"[SecuritySync] Synced {result.Securities.Count} securities, saved lastSyncTime={result.ServerTime}");

            return new SecuritySyncResult
            {
                SyncedCount = result.Securities.Count,
                IsFullSync = result.IsFullSync,
                LastSyncTime = result.ServerTime
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SecuritySync] FAILED: {ex.GetType().Name}: {ex.Message}");
            return new SecuritySyncResult { LastSyncTime = lastSyncTime };
        }
    }
}
