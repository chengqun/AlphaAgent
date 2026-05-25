using System.Globalization;

namespace AlphaAgent.Maui.Converters;

public class BooleanToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isTrue = value is bool && (bool)value;
        string[] colors = parameter?.ToString()?.Split(',') ?? ["#FF5722", "#666666"];
        
        return Color.FromArgb(isTrue ? colors[0].Trim() : colors[1].Trim());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}