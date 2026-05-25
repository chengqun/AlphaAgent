using Microsoft.Maui.Controls;

namespace AlphaAgent.Maui.Services;

public enum ThemeMode
{
    System,
    Light,
    Dark
}

public interface IThemeManager
{
    ThemeMode CurrentTheme { get; }
    event EventHandler<ThemeMode>? ThemeChanged;
    Task SetThemeAsync(ThemeMode themeMode);
    Task<ThemeMode> GetSavedThemeAsync();
    void Initialize();
}

public class ThemeManager : IThemeManager
{
    private const string ThemePreferenceKey = "AlphaAgent_ThemeMode";
    private ThemeMode _currentTheme;

    public ThemeMode CurrentTheme
    {
        get => _currentTheme;
        private set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                OnThemeChanged(value);
            }
        }
    }

    public event EventHandler<ThemeMode>? ThemeChanged;

    public ThemeManager()
    {
        _currentTheme = GetSavedTheme();
    }

    private ThemeMode GetSavedTheme()
    {
        if (Preferences.ContainsKey(ThemePreferenceKey))
        {
            var saved = Preferences.Get(ThemePreferenceKey, (int)ThemeMode.System);
            return (ThemeMode)saved;
        }
        return ThemeMode.System;
    }

    public async Task<ThemeMode> GetSavedThemeAsync()
    {
        await Task.CompletedTask;
        return CurrentTheme;
    }

    public async Task SetThemeAsync(ThemeMode themeMode)
    {
        CurrentTheme = themeMode;
        Preferences.Set(ThemePreferenceKey, (int)themeMode);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ApplyUserAppTheme(themeMode);
        });

        await Task.CompletedTask;
    }

    private void ApplyUserAppTheme(ThemeMode themeMode)
    {
        switch (themeMode)
        {
            case ThemeMode.Light:
                App.Current!.UserAppTheme = AppTheme.Light;
                break;
            case ThemeMode.Dark:
                App.Current!.UserAppTheme = AppTheme.Dark;
                break;
            case ThemeMode.System:
                App.Current!.UserAppTheme = AppTheme.Unspecified;
                break;
        }
    }

    protected virtual void OnThemeChanged(ThemeMode themeMode)
    {
        ThemeChanged?.Invoke(this, themeMode);
    }

    public void Initialize()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ApplyUserAppTheme(CurrentTheme);
        });
    }
}
