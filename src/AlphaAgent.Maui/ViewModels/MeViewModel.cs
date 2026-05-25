using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Auth;
using AlphaAgent.Application.Dtos.Auth;
using AlphaAgent.Maui.Services;
using System.Collections.ObjectModel;

namespace AlphaAgent.Maui.ViewModels;

public partial class MeViewModel : ObservableObject
{
    private readonly IAuthService? _authService;
    private readonly IThemeManager? _themeManager;

    [ObservableProperty]
    private string _username = "未登录";

    [ObservableProperty]
    private string _usernameInitial = "?";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ThemeMode _selectedThemeMode;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private ObservableCollection<AccountInfoDto> _storedAccounts = [];

    public string ThemeModeText => IsDarkMode ? "深色模式" : "浅色模式";

    public string ThemeIcon => IsDarkMode ? "🌙" : "☀";

    partial void OnIsDarkModeChanged(bool value)
    {
        OnPropertyChanged(nameof(ThemeModeText));
        OnPropertyChanged(nameof(ThemeIcon));
    }

    public MeViewModel(IAuthService? authService = null, IThemeManager? themeManager = null)
    {
        _authService = authService;
        _themeManager = themeManager;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        await LoadThemeModeAsync();
        await LoadUserInfoAsync();
    }

    private async Task LoadThemeModeAsync()
    {
        if (_themeManager != null)
        {
            SelectedThemeMode = await _themeManager.GetSavedThemeAsync();
            IsDarkMode = SelectedThemeMode == ThemeMode.Dark;
        }
    }

    private async Task LoadUserInfoAsync()
    {
        IsLoading = true;
        try
        {
            if (_authService != null)
            {
                var username = await _authService.GetUsernameAsync();

                Username = !string.IsNullOrEmpty(username) ? username : "未登录";
                UsernameInitial = !string.IsNullOrEmpty(username) && username.Length > 0
                    ? username.Trim().ToUpper().FirstOrDefault().ToString()
                    : "?";

                // 加载已保存的账号列表
                var accounts = await _authService.GetStoredAccountsAsync();
                Console.WriteLine($"[MeViewModel] Found {accounts.Count} stored accounts");
                foreach (var account in accounts)
                {
                    Console.WriteLine($"[MeViewModel]   - {account.Username} (Active: {account.IsActive})");
                }
                
                // 确保正确更新 ObservableCollection
                StoredAccounts.Clear();
                foreach (var account in accounts)
                {
                    StoredAccounts.Add(account);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MeViewModel] Error loading user info: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        IsLoading = true;
        try
        {
            if (_authService != null)
            {
                await _authService.LogoutAsync();
            }

            // 无论如何，导航到登录页面
            Username = "未登录";
            UsernameInitial = "?";
            StoredAccounts.Clear();

            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MeViewModel] Error during logout: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SwitchAccountAsync(string username)
    {
        if (_authService == null) return;

        IsLoading = true;
        try
        {
            var response = await _authService.SwitchAccountAsync(username);
            if (response.Success)
            {
                await LoadUserInfoAsync();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ToggleThemeAsync(bool isDark)
    {
        try
        {
            if (_themeManager != null)
            {
                var themeMode = isDark ? ThemeMode.Dark : ThemeMode.Light;
                await _themeManager.SetThemeAsync(themeMode);
                SelectedThemeMode = themeMode;
                IsDarkMode = isDark;
            }
        }
        catch
        {
        }
    }
}
