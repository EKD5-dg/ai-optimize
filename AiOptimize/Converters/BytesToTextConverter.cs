using System.Globalization;
using System.Windows.Data;
using AiOptimize.Utils;

namespace AiOptimize.Converters;

public sealed class BytesToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        long l => ByteFormatter.Format(l),
        ulong u => ByteFormatter.Format((double)u),
        double d => ByteFormatter.Format(d),
        int i => ByteFormatter.Format(i),
        _ => "0 B",
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
