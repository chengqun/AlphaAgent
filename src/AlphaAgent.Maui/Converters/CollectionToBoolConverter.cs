using System.Collections;
using System.Globalization;

namespace AlphaAgent.Maui.Converters;

public class CollectionToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ICollection collection)
            return collection.Count > 0;

        if (value is IEnumerable enumerable)
            return enumerable.GetEnumerator().MoveNext();

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}