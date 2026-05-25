using System.Globalization;

namespace AlphaAgent.Maui.Converters;

public class RoleToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var role = value as string;
        var targetRole = parameter as string;
        
        return string.Equals(role, targetRole, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}