using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AiOptimize.Converters;

public sealed class UsageToBrushConverter : IValueConverter
{
    private static readonly Brush Normal = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
    private static readonly Brush Danger = new SolidColorBrush(Color.FromRgb(0xEF, 0x53, 0x50));

    static UsageToBrushConverter()
    {
        Normal.Freeze();
        Danger.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double usage && usage >= 90 ? Danger : Normal;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
