using System.Globalization;

namespace AlphaAgent.Maui.Converters;

public class NullToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool result = value != null;
        
        // 对于数值类型，检查是否为默认值（0）
        if (value is int intValue)
        {
            result = intValue > 0;
        }
        else if (value is long longValue)
        {
            result = longValue > 0;
        }
        else if (value is double doubleValue)
        {
            result = doubleValue > 0;
        }
        else if (value is float floatValue)
        {
            result = floatValue > 0;
        }
        
        // 支持反向逻辑
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