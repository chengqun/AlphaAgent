using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AlphaAgent.Abp.Application.Contracts.Services.Security;
using AlphaAgent.Abp.Domain.Entities;
using AlphaAgent.Abp.Domain.Services.Moment;
using AlphaAgent.Abp.Domain.Services.Securities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace AlphaAgent.Abp.Application.Services.Security;

public class SecuritySyncService : ApplicationService, ISecuritySyncService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecurityManager _securityManager;
    private readonly IMomentManager _momentManager;
    private readonly IRepository<AppMoment, Guid> _momentRepository;
    private readonly SecuritySyncOptions _options;

    public SecuritySyncService(
        IHttpClientFactory httpClientFactory,
        ISecurityManager securityManager,
        IMomentManager momentManager,
        IRepository<AppMoment, Guid> momentRepository,
        IOptions<SecuritySyncOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _securityManager = securityManager;
        _momentManager = momentManager;
        _momentRepository = momentRepository;
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

    public async Task<StockPickingMomentResult> SyncStockPickingMomentsAsync()
    {
        const string stockPickingUrl = "http://ai.10jqka.com.cn/transfer/index/index?app=19";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            var response = await client.GetStringAsync(stockPickingUrl);

            var strategies = ParseStockPickingData(response);
            if (strategies.Count == 0)
            {
                Logger.LogInformation("StockPickingSync: 外部数据源返回0条策略");
                return new StockPickingMomentResult();
            }

            // 取策略日期（所有策略同一天）
            var pickingDate = strategies.First().Date;
            var dateStart = pickingDate.Date;
            var dateEnd = dateStart.AddDays(1);

            // 查询该日期已有的股票朋友圈，用于去重（内容相同则跳过）
            var existingMoments = await _momentRepository.GetListAsync(m =>
                m.Type == "Stock" && m.CreatedAt >= dateStart && m.CreatedAt < dateEnd);
            // 用内容做去重集合：同一天内容已存在则跳过
            var existingContents = existingMoments.Select(m => m.Content).ToHashSet();

            var result = new StockPickingMomentResult { TotalStrategies = strategies.Count };

            // 每个策略的每只股票各发一条朋友圈
            foreach (var strategy in strategies)
            {
                var content = $"🎯 击中策略：{strategy.StrategyName}";

                // 去重：同一天内容相同则跳过
                if (existingContents.Contains(content))
                {
                    continue;
                }

                foreach (var stock in strategy.Stocks)
                {
                    // 按 code + type 精确查找，不加载全表
                    var security = await _securityManager.GetByCodeAndTypeAsync(stock.StockCode, "股票");
                    if (security == null)
                    {
                        Logger.LogWarning("StockPickingSync: 股票代码 {Code} (Type=股票) 未在 AppSecurities 中找到，跳过", stock.StockCode);
                        result.SkippedStocks++;
                        continue;
                    }

                    await _momentManager.CreateStockMomentAsync(security.Id, content, strategy.Date);
                    result.PublishedMoments++;
                }

                // 记录已发布的内容，防止同一策略在同一次运行中重复
                existingContents.Add(content);
            }

            Logger.LogInformation("StockPickingSync: 同步完成，{Strategies}个策略，发布{Published}条朋友圈，跳过{Skipped}只股票",
                result.TotalStrategies, result.PublishedMoments, result.SkippedStocks);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "StockPickingSync: 同步失败 - {Message}", ex.Message);
            return new StockPickingMomentResult();
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

    private List<StockPickingStrategy> ParseStockPickingData(string response)
    {
        using var doc = JsonDocument.Parse(response);

        if (!doc.RootElement.TryGetProperty("data", out var dataArray))
        {
            return new List<StockPickingStrategy>();
        }

        var result = new List<StockPickingStrategy>();

        foreach (var item in dataArray.EnumerateArray())
        {
            var strategyName = item.TryGetProperty("strategy_name", out var sn) ? sn.GetString() ?? "" : "";
            var dateStr = item.TryGetProperty("stockpicking_date", out var d) ? d.GetString() ?? "" : "";

            if (!item.TryGetProperty("stock_info", out var stockInfoArray))
                continue;

            var stocks = new List<StockPickingStock>();
            foreach (var stockItem in stockInfoArray.EnumerateArray())
            {
                var code = stockItem.TryGetProperty("stock_code", out var sc) ? sc.GetString() ?? "" : "";
                var name = stockItem.TryGetProperty("stock_name", out var sn2) ? sn2.GetString() ?? "" : "";
                if (!string.IsNullOrEmpty(code))
                    stocks.Add(new StockPickingStock { StockCode = code, StockName = name });
            }

            // 解析日期：yyyyMMdd → DateTime
            DateTime date = default;
            if (dateStr.Length == 8 && int.TryParse(dateStr, out _))
            {
                date = new DateTime(
                    int.Parse(dateStr[..4]),
                    int.Parse(dateStr[4..6]),
                    int.Parse(dateStr[6..8]));
            }
            else
            {
                date = DateTime.UtcNow;
            }

            if (!string.IsNullOrEmpty(strategyName) && stocks.Count > 0)
            {
                result.Add(new StockPickingStrategy
                {
                    StrategyName = strategyName,
                    Date = date,
                    Stocks = stocks
                });
            }
        }

        return result;
    }
}

/// <summary>
/// 选股策略数据
/// </summary>
internal class StockPickingStrategy
{
    public string StrategyName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<StockPickingStock> Stocks { get; set; } = new();
}

/// <summary>
/// 选股策略中的股票
/// </summary>
internal class StockPickingStock
{
    public string StockCode { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
}
