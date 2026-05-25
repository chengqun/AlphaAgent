using System;
using System.Globalization;

namespace AlphaAgent.Maui.Converters;

public class UtcToLocalTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            // 服务端返回的 DateTime 经 JSON 反序列化后 Kind 通常为 Unspecified，
            // 需要将其视为 UTC 再转为本地时间
            var localTime = dateTime.Kind == DateTimeKind.Local
                ? dateTime
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();

            if (parameter is not null)
                return localTime.ToString(parameter.ToString()!, culture);

            var now = DateTime.Now;
            var today = now.Date;
            var date = localTime.Date;

            if (date == today)
                return localTime.ToString("HH:mm", culture);
            if (date == today.AddDays(-1))
                return "昨天";
            if (date.Year == now.Year)
                return localTime.ToString("MM/dd", culture);
            return localTime.ToString("yy/MM/dd", culture);
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
