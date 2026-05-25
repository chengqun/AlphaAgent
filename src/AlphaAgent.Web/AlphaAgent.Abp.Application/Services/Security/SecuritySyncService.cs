using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AlphaAgent.Abp.Application.Contracts.Services.Security;
using AlphaAgent.Abp.Domain.Entities;
using AlphaAgent.Abp.Domain.Services.Securities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Application.Services;

namespace AlphaAgent.Abp.Application.Services.Security;

public class SecuritySyncService : ApplicationService, ISecuritySyncService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecurityManager _securityManager;
    private readonly SecuritySyncOptions _options;

    public SecuritySyncService(
        IHttpClientFactory httpClientFactory,
        ISecurityManager securityManager,
        IOptions<SecuritySyncOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _securityManager = securityManager;
        _options = options.Value;
    }

    public async Task<SecuritySyncResult> SyncFromExternalAsync()
    {
        if (string.IsNullOrEmpty(_options.Url))
        {
            Logger.LogWarning("SecuritySync: 未配置外部数据源URL，跳过同步");
            return new SecuritySyncResult();
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            var response = await client.GetStringAsync(_options.Url);
            var securities = ParseFutureData(response);

            if (securities.Count == 0)
            {
                Logger.LogInformation("SecuritySync: 外部数据源返回0条记录");
                return new SecuritySyncResult { Total = 0 };
            }

            var (added, updated) = await _securityManager.UpsertRangeAsync(securities);
            Logger.LogInformation("SecuritySync: 同步完成，共{Total}条，新增{Added}条，更新{Updated}条",
                securities.Count, added, updated);

            return new SecuritySyncResult
            {
                Total = securities.Count,
                Added = added,
                Updated = updated
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SecuritySync: 同步失败 - {Message}", ex.Message);
            return new SecuritySyncResult();
        }
    }

    private List<AppSecurity> ParseFutureData(string response)
    {
        using var doc = JsonDocument.Parse(response);

        if (!doc.RootElement.TryGetProperty("list", out var listElement))
        {
            return new List<AppSecurity>();
        }

        return listElement.EnumerateArray()
            .Select(item =>
            {
                var code = item.TryGetProperty("dm", out var dm) ? dm.GetString()?.ToLower() ?? "" : "";
                var name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
                var sc = item.TryGetProperty("sc", out var scEl) ? scEl.GetInt32() : 0;

                return new AppSecurity
                {
                    Code = code,
                    Name = name,
                    Type = "期货",
                    Exchange = sc.ToString(),
                    BaseCode = ExtractBaseCode(code)
                };
            })
            .Where(s => !string.IsNullOrEmpty(s.Code))
            .ToList();
    }

    private static string ExtractBaseCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return code;

        var i = code.Length - 1;
        while (i >= 0 && char.IsDigit(code[i])) i--;

        return i < code.Length - 1 ? code[..(i + 1)] : code;
    }
}
