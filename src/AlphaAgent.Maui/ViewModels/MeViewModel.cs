using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Auth;
using AlphaAgent.Maui.Services;

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
            var themeMode = await Task.Run(async () => await _themeManager.GetSavedThemeAsync());
            SelectedThemeMode = themeMode;
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
                var username = await Task.Run(async () => await _authService.GetUsernameAsync());

                Username = !string.IsNullOrEmpty(username) ? username : "未登录";
                UsernameInitial = !string.IsNullOrEmpty(username) && username.Length > 0
                    ? username.Trim().ToUpper().FirstOrDefault().ToString()
                    : "?";
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
                await Task.Run(() => _authService.LogoutAsync());
            }

            Username = "未登录";
            UsernameInitial = "?";

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
    private async Task NavigateToDeviceManagementAsync()
    {
        await Shell.Current.GoToAsync("DeviceManagementPage");
    }

    [RelayCommand]
    private async Task NavigateToAiSettingsAsync()
    {
        await Shell.Current.GoToAsync("AiSettingsPage");
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
