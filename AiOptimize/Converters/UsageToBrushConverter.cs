using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AiOptimize.Converters;

[ValueConversion(typeof(double), typeof(Brush))]
public sealed class UsageToBrushConverter : IValueConverter
{
    private static readonly Brush Normal = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
    private static readonly Brush Danger = new SolidColorBrush(Color.FromRgb(0xEF, 0x53, 0x50));

    static UsageToBrushConverter()
    {
        Normal.Freeze();
        Danger.Freeze();
    }

    /// <summary>告警阈值，可用 ConverterParameter 覆盖（默认 90）。</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null or DependencyProperty) return DependencyProperty.UnsetValue;
        if (value is not IConvertible convertible) return DependencyProperty.UnsetValue;

        double usage = convertible.ToDouble(culture);
        double threshold = 90;
        if (parameter is IConvertible p) threshold = p.ToDouble(culture);
        return usage >= threshold ? Danger : Normal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
