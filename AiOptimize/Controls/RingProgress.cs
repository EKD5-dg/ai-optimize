using System.Windows;
using System.Windows.Media;

namespace AiOptimize.Controls;

/// <summary>环形进度指示器，Value 取值 0-100。</summary>
public sealed class RingProgress : FrameworkElement
{
    private static readonly Brush DefaultTrackBrush = CreateFrozenTrackBrush();

    private static Brush CreateFrozenTrackBrush()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x50));
        brush.Freeze(); // DP 默认值全局共享，必须冻结避免跨实例污染与线程亲和问题
        return brush;
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(RingProgress),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender),
        value => value is double d && !double.IsNaN(d) && !double.IsInfinity(d));

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness), typeof(double), typeof(RingProgress),
        new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsRender),
        value => value is double d && !double.IsNaN(d) && !double.IsInfinity(d));

    public static readonly DependencyProperty RingBrushProperty = DependencyProperty.Register(
        nameof(RingBrush), typeof(Brush), typeof(RingProgress),
        new FrameworkPropertyMetadata(Brushes.DeepSkyBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(RingProgress),
        new FrameworkPropertyMetadata(DefaultTrackBrush, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double Thickness { get => (double)GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }
    public Brush RingBrush { get => (Brush)GetValue(RingBrushProperty); set => SetValue(RingBrushProperty, value); }
    public Brush TrackBrush { get => (Brush)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }

    /// <summary>无约束时给出合理默认尺寸，避免在 Auto 容器中完全不可见。</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        double fallback = Math.Max(Thickness * 4, 48);
        double width = double.IsInfinity(availableSize.Width) ? fallback : availableSize.Width;
        double height = double.IsInfinity(availableSize.Height) ? fallback : availableSize.Height;
        double side = Math.Min(width, height);
        return new Size(side, side);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0 || double.IsNaN(size)) return;
        double thickness = Math.Clamp(Thickness, 0, size / 2);
        if (thickness <= 0) return;
        double radius = (size - thickness) / 2;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);

        dc.DrawEllipse(null, new Pen(TrackBrush, thickness), center, radius, radius);

        double value = Math.Clamp(Value, 0, 100);
        if (value <= 0 || double.IsNaN(value)) return;

        var pen = new Pen(RingBrush, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
        };

        if (value >= 99.99)
        {
            dc.DrawEllipse(null, pen, center, radius, radius);
            return;
        }

        // 起点 12 点方向，顺时针画弧
        double angle = value / 100 * 360;
        double radians = (angle - 90) * Math.PI / 180;
        var start = new Point(center.X, center.Y - radius);
        var end = new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(start, false, false);
            ctx.ArcTo(end, new Size(radius, radius), 0, angle > 180, SweepDirection.Clockwise, true, false);
        }
        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }
}
