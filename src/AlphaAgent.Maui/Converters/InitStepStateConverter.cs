using System.Globalization;
using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Converters;

public class InitStepStateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is InitStepState state && parameter is string targetState)
            return state.ToString() == targetState;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}