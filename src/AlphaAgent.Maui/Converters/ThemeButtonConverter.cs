using System.Globalization;
using AlphaAgent.Maui.Services;

namespace AlphaAgent.Maui.Converters;

public class ThemeButtonConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ThemeMode selectedTheme && parameter is string targetTheme)
        {
            if (Enum.TryParse<ThemeMode>(targetTheme, out var parsedTheme) && selectedTheme == parsedTheme)
            {
                return App.Current?.Resources["Primary"] as Color ?? Colors.Gold;
            }
        }
        bool isDark = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
        return App.Current?.Resources[isDark ? "AppBackgroundDark" : "AppBackgroundLight"] as Color ?? Colors.LightGray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ThemeButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ThemeMode selectedTheme && parameter is string targetTheme)
        {
            if (Enum.TryParse<ThemeMode>(targetTheme, out var parsedTheme) && selectedTheme == parsedTheme)
            {
                return App.Current?.Resources["TextWhite"] as Color ?? Colors.White;
            }
        }
        bool isDark = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
        return App.Current?.Resources[isDark ? "TextPrimaryDark" : "TextPrimaryLight"] as Color ?? Colors.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
