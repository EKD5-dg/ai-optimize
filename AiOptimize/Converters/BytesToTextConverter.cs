using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AiOptimize.Utils;

namespace AiOptimize.Converters;

public sealed class BytesToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 绑定错误/数据未就绪时透传 UnsetValue，让 FallbackValue 生效并暴露绑定问题
        if (value is null) return string.Empty;
        if (ReferenceEquals(value, DependencyProperty.UnsetValue)) return DependencyProperty.UnsetValue;
        return value switch
        {
        long l => ByteFormatter.Format(l),
        ulong u => ByteFormatter.Format(u),
        double d => ByteFormatter.Format(d),
        int i => ByteFormatter.Format(i),
        uint ui => ByteFormatter.Format(ui),
        short s => ByteFormatter.Format(s),
        ushort us => ByteFormatter.Format(us),
        byte b => ByteFormatter.Format(b),
        float f => ByteFormatter.Format(f),
        decimal m => ByteFormatter.Format((double)m),
        IConvertible c => ByteFormatter.Format(c.ToDouble(culture)),
        _ => DependencyProperty.UnsetValue,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
