using System;
using System.Threading;
using System.Threading.Tasks;
using AlphaAgent.Abp.Application.Contracts.Services.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlphaAgent.Abp.Application.Services.Security;

public class SecuritySyncWorker : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SecuritySyncOptions _options;
    private readonly ILogger<SecuritySyncWorker> _logger;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;

    public SecuritySyncWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<SecuritySyncOptions> options,
        ILogger<SecuritySyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.Url))
        {
            _logger.LogInformation("SecuritySyncWorker: 未配置同步URL，不启动定时同步");
            return Task.CompletedTask;
        }

        _logger.LogInformation("SecuritySyncWorker: 启动定时同步，间隔{Interval}分钟，URL: {Url}",
            _options.IntervalMinutes, _options.Url);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IntervalMinutes));

        _ = ExecuteAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SecuritySyncWorker: 停止定时同步");
        _cts?.Cancel();
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // 启动时立即执行一次
        try
        {
            await SyncInScopeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SecuritySyncWorker: 初始同步失败");
        }

        // 定时循环
        while (_timer != null && await _timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await SyncInScopeAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecuritySyncWorker: 定时同步失败");
            }
        }
    }

    private async Task SyncInScopeAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<ISecuritySyncService>();
        await syncService.SyncFromExternalAsync();
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
