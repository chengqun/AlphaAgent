using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Auth;
using AlphaAgent.Application.Interfaces.Common;
using AlphaAgent.Application.Interfaces.Update;
using AlphaAgent.Application.Dtos.Update;
using AlphaAgent.Domain.Abstractions.Enums;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Maui.Services;
using AlphaAgent.Maui.Events;

namespace AlphaAgent.Maui.ViewModels;

public partial class SplashViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ICoreInitializer _coreInitializer;
    private readonly IThemeManager _themeManager;
    private readonly IUpdateService? _updateService;

    [ObservableProperty]
    private string _statusMessage = "正在初始化...";

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _hasUpdate;

    [ObservableProperty]
    private CheckUpdateResultDto? _updateInfo;

    public event EventHandler<SplashNavigationEventArgs>? InitializationCompleted;

    public SplashViewModel(IAuthService authService,
                          ICoreInitializer coreInitializer,
                          IThemeManager themeManager,
                          IUpdateService? updateService = null)
    {
        _authService = authService;
        _coreInitializer = coreInitializer;
        _themeManager = themeManager;
        _updateService = updateService;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        bool isLoggedIn = false;

        try
        {
            StatusMessage = "正在初始化...";
            await Task.Delay(300);

            StatusMessage = "正在初始化主题...";
            _themeManager.Initialize();
            await Task.Delay(300);

            StatusMessage = "正在初始化数据库...";
            await _coreInitializer.InitializeAsync();
            await Task.Delay(300);

            StatusMessage = "正在检查登录状态...";
            await Task.Delay(300);

            StatusMessage = "正在自动登录...";
            isLoggedIn = await _authService.IsLoggedInAsync();

            StatusMessage = "初始化完成";
            await Task.Delay(300);

            await CheckUpdateAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"初始化失败: {ex.Message}";
            await Task.Delay(1000);
        }
        finally
        {
            IsLoading = false;
            InitializationCompleted?.Invoke(this, new SplashNavigationEventArgs { IsLoggedIn = isLoggedIn });
        }
    }

    private async Task CheckUpdateAsync()
    {
        if (_updateService == null) return;

        try
        {
            StatusMessage = "正在检查更新...";

            var platform = GetAppPlatform();
            var versionCode = GetVersionCode();

            var result = await _updateService.CheckUpdateAsync(platform, versionCode);
            if (result.Success && result.Data?.HasUpdate == true)
            {
                HasUpdate = true;
                UpdateInfo = result.Data;
                System.Diagnostics.Debug.WriteLine($"[SplashViewModel] 发现新版本: {result.Data.VersionName} ({result.Data.VersionCode})");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SplashViewModel] 检查更新失败: {ex.Message}");
        }
    }

    private static AppPlatform GetAppPlatform()
    {
        var platform = DeviceInfo.Platform;
        if (platform == DevicePlatform.iOS) return AppPlatform.iOS;
        if (platform == DevicePlatform.Android) return AppPlatform.Android;
        if (platform == DevicePlatform.WinUI) return AppPlatform.Windows;
        if (platform == DevicePlatform.macOS) return AppPlatform.MacCatalyst;
        return AppPlatform.Android;
    }

    private static int GetVersionCode()
    {
        return int.TryParse(AppInfo.Current.BuildString, out var code) ? code : 1;
    }
}