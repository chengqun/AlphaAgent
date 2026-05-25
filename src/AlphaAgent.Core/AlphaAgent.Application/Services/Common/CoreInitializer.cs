using AlphaAgent.Application.Interfaces.Common;
using AlphaAgent.Application.Interfaces.Security;
using AlphaAgent.Domain.Abstractions;
using System;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Common;

public class CoreInitializer : ICoreInitializer
{
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly ISecurityClientSyncService _securityClientSyncService;

    public CoreInitializer(
        IDatabaseInitializer databaseInitializer,
        ISecurityClientSyncService securityClientSyncService)
    {
        _databaseInitializer = databaseInitializer;
        _securityClientSyncService = securityClientSyncService;
    }

    public async Task InitializeAsync()
    {
        await _databaseInitializer.InitializeAsync();

        try
        {
            Console.WriteLine("[CoreInitializer] Starting security sync...");
            var result = await _securityClientSyncService.SyncFromServerAsync();
            Console.WriteLine($"[CoreInitializer] Security sync result: SyncedCount={result.SyncedCount}, IsFullSync={result.IsFullSync}, LastSyncTime={result.LastSyncTime}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CoreInitializer] Security sync failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
