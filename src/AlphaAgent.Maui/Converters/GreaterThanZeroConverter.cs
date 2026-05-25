using System.Globalization;

namespace AlphaAgent.Maui.Converters;

public class GreaterThanZeroConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool result = value is int intValue && intValue > 0;

        if (parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
        {
            result = !result;
        }

        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}