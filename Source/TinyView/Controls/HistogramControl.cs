using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TinyView.Models;

namespace TinyView.Controls;

/// <summary>
/// Draws a <see cref="Models.Histogram"/> with draggable black/white point markers.
/// The markers edit <see cref="BlackPoint"/>/<see cref="WhitePoint"/> (two-way by default);
/// while dragging, writes are coalesced to at most one per rendered frame so a costly
/// re-render of the bound image happens at most once per frame.
/// </summary>
public sealed class HistogramControl : FrameworkElement
{
    private const double StripHeight = 10;
    private const double HandleHeight = 8;
    private const double Gap = 2;
    private const double GrabDistance = 6;

    private enum Marker { None, Black, White }

    #region Dependency properties

    public static readonly DependencyProperty HistogramProperty = DependencyProperty.Register(
        nameof(Histogram), typeof(Histogram), typeof(HistogramControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public Histogram? Histogram
    {
        get => (Histogram?)GetValue(HistogramProperty);
        set => SetValue(HistogramProperty, value);
    }

    /// <summary>Lowest value a marker can be dragged to (the image minimum).</summary>
    public static readonly DependencyProperty RangeMinimumProperty = DependencyProperty.Register(
        nameof(RangeMinimum), typeof(double), typeof(HistogramControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double RangeMinimum
    {
        get => (double)GetValue(RangeMinimumProperty);
        set => SetValue(RangeMinimumProperty, value);
    }

    /// <summary>Highest value a marker can be dragged to (the image maximum).</summary>
    public static readonly DependencyProperty RangeMaximumProperty = DependencyProperty.Register(
        nameof(RangeMaximum), typeof(double), typeof(HistogramControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double RangeMaximum
    {
        get => (double)GetValue(RangeMaximumProperty);
        set => SetValue(RangeMaximumProperty, value);
    }

    public static readonly DependencyProperty BlackPointProperty = DependencyProperty.Register(
        nameof(BlackPoint), typeof(double), typeof(HistogramControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public double BlackPoint
    {
        get => (double)GetValue(BlackPointProperty);
        set => SetValue(BlackPointProperty, value);
    }

    public static readonly DependencyProperty WhitePointProperty = DependencyProperty.Register(
        nameof(WhitePoint), typeof(double), typeof(HistogramControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public double WhitePoint
    {
        get => (double)GetValue(WhitePointProperty);
        set => SetValue(WhitePointProperty, value);
    }

    public static readonly DependencyProperty IsLogScaleProperty = DependencyProperty.Register(
        nameof(IsLogScale), typeof(bool), typeof(HistogramControl),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsLogScale
    {
        get => (bool)GetValue(IsLogScaleProperty);
        set => SetValue(IsLogScaleProperty, value);
    }

    /// <summary>Palette shown in the gradient strip, stretched between the black and white points.</summary>
    public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
        nameof(Palette), typeof(BitmapPalette), typeof(HistogramControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPaletteChanged));

    public BitmapPalette? Palette
    {
        get => (BitmapPalette?)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    #endregion

    private GradientStopCollection? _paletteStops;

    private Marker _dragMarker = Marker.None;
    private double _dragValue;
    private bool _commitPending;

    static HistogramControl()
    {
        FocusableProperty.OverrideMetadata(typeof(HistogramControl), new FrameworkPropertyMetadata(false));
        ClipToBoundsProperty.OverrideMetadata(typeof(HistogramControl), new FrameworkPropertyMetadata(true));
    }

    private static void OnPaletteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (HistogramControl)d;
        control._paletteStops = null;
        if (e.NewValue is BitmapPalette palette && palette.Colors.Count > 0)
        {
            var stops = new GradientStopCollection(palette.Colors.Count);
            double last = Math.Max(1, palette.Colors.Count - 1);
            for (int i = 0; i < palette.Colors.Count; i++)
                stops.Add(new GradientStop(palette.Colors[i], i / last));
            stops.Freeze();
            control._paletteStops = stops;
        }
    }

    // Marker values as currently displayed (the in-flight drag value overrides the bound one).
    private double DisplayedBlack => _dragMarker == Marker.Black ? _dragValue : BlackPoint;
    private double DisplayedWhite => _dragMarker == Marker.White ? _dragValue : WhitePoint;

    private double PlotBottom => Math.Max(0, ActualHeight - StripHeight - HandleHeight - Gap);

    private double ToX(double value)
    {
        var h = Histogram;
        return h == null ? 0 : HistogramMapping.ValueToX(value, h.Range, ActualWidth);
    }

    private double ToValue(double x)
    {
        var h = Histogram;
        return h == null ? 0 : HistogramMapping.XToValue(x, h.Range, ActualWidth);
    }

    protected override Size MeasureOverride(Size availableSize) => new(0, StripHeight + HandleHeight + Gap + 20);

    protected override void OnRender(DrawingContext dc)
    {
        double width = ActualWidth;
        double height = ActualHeight;
        double plotBottom = PlotBottom;
        double stripTop = plotBottom + Gap;
        double handleTop = stripTop + StripHeight;

        dc.DrawRectangle(SystemColors.WindowBrush, null, new Rect(0, 0, width, height));

        var histogram = Histogram;
        if (histogram == null || histogram.Counts.Length == 0 || width <= 0)
            return;

        DrawBars(dc, histogram, width, plotBottom);

        double blackX = Math.Clamp(ToX(DisplayedBlack), 0, width);
        double whiteX = Math.Clamp(ToX(DisplayedWhite), 0, width);

        // Dim the parts of the distribution that are clipped to the first/last palette color.
        var shade = new SolidColorBrush(Color.FromArgb(0x50, 0x80, 0x80, 0x80));
        shade.Freeze();
        if (blackX > 0)
            dc.DrawRectangle(shade, null, new Rect(0, 0, blackX, plotBottom));
        if (whiteX < width)
            dc.DrawRectangle(shade, null, new Rect(whiteX, 0, width - whiteX, plotBottom));

        // Palette strip: the colors the image actually uses for each raw value.
        var stripRect = new Rect(0, stripTop, width, StripHeight);
        if (_paletteStops != null)
        {
            var brush = new LinearGradientBrush(_paletteStops)
            {
                MappingMode = BrushMappingMode.Absolute,
                StartPoint = new Point(blackX, 0),
                EndPoint = new Point(Math.Max(whiteX, blackX + 0.001), 0),
                SpreadMethod = GradientSpreadMethod.Pad,
            };
            dc.DrawRectangle(brush, null, stripRect);
        }
        dc.DrawRectangle(null, new Pen(SystemColors.ControlDarkBrush, 1), stripRect);

        DrawMarker(dc, blackX, plotBottom, handleTop, Brushes.Black);
        DrawMarker(dc, whiteX, plotBottom, handleTop, Brushes.White);
    }

    private void DrawBars(DrawingContext dc, Histogram histogram, double width, double plotBottom)
    {
        if (histogram.MaxCount <= 0 || plotBottom <= 0)
            return;

        int bins = histogram.Counts.Length;
        double binWidth = width / bins;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(0, plotBottom), isFilled: true, isClosed: true);
            for (int i = 0; i < bins; i++)
            {
                double y = plotBottom - HistogramMapping.NormalizedHeight(histogram.Counts[i], histogram.MaxCount, IsLogScale) * plotBottom;
                ctx.LineTo(new Point(i * binWidth, y), isStroked: false, isSmoothJoin: false);
                ctx.LineTo(new Point((i + 1) * binWidth, y), isStroked: false, isSmoothJoin: false);
            }
            ctx.LineTo(new Point(width, plotBottom), isStroked: false, isSmoothJoin: false);
        }
        geometry.Freeze();

        dc.DrawGeometry(SystemColors.ControlDarkDarkBrush, null, geometry);
    }

    private static void DrawMarker(DrawingContext dc, double x, double plotBottom, double handleTop, Brush fill)
    {
        var linePen = new Pen(SystemColors.HighlightBrush, 1);
        dc.DrawLine(linePen, new Point(x, 0), new Point(x, plotBottom));

        var triangle = new StreamGeometry();
        using (var ctx = triangle.Open())
        {
            ctx.BeginFigure(new Point(x, handleTop), isFilled: true, isClosed: true);
            ctx.LineTo(new Point(x - HandleHeight * 0.75, handleTop + HandleHeight), isStroked: true, isSmoothJoin: false);
            ctx.LineTo(new Point(x + HandleHeight * 0.75, handleTop + HandleHeight), isStroked: true, isSmoothJoin: false);
        }
        triangle.Freeze();
        dc.DrawGeometry(fill, new Pen(Brushes.Gray, 1), triangle);
    }

    #region Mouse interaction

    /// <summary>Marker within grab distance of <paramref name="x"/>, preferring the closer one.</summary>
    private Marker HitTestMarker(double x)
    {
        double width = ActualWidth;
        double blackDist = Math.Abs(Math.Clamp(ToX(BlackPoint), 0, width) - x);
        double whiteDist = Math.Abs(Math.Clamp(ToX(WhitePoint), 0, width) - x);

        if (blackDist > GrabDistance && whiteDist > GrabDistance)
            return Marker.None;

        // Overlapping markers: pick by drag direction side (left of the pair → black).
        if (blackDist == whiteDist)
            return x < Math.Clamp(ToX(BlackPoint), 0, width) ? Marker.Black : Marker.White;

        return blackDist < whiteDist ? Marker.Black : Marker.White;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (Histogram == null || !IsEnabled)
            return;

        var pos = e.GetPosition(this);
        var marker = HitTestMarker(pos.X);

        // Clicking the strip/handle area away from a marker moves the nearest marker there.
        if (marker == Marker.None && pos.Y >= PlotBottom)
        {
            double blackDist = Math.Abs(ToX(BlackPoint) - pos.X);
            double whiteDist = Math.Abs(ToX(WhitePoint) - pos.X);
            marker = blackDist <= whiteDist ? Marker.Black : Marker.White;
        }

        if (marker == Marker.None)
            return;

        if (e.ClickCount == 2)
        {
            // Double-click resets the marker to the image range.
            if (marker == Marker.Black)
                SetCurrentValue(BlackPointProperty, RangeMinimum);
            else
                SetCurrentValue(WhitePointProperty, RangeMaximum);
            e.Handled = true;
            return;
        }

        _dragMarker = marker;
        _dragValue = marker == Marker.Black ? BlackPoint : WhitePoint;
        CaptureMouse();
        UpdateDrag(pos.X);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (Histogram == null)
            return;

        var pos = e.GetPosition(this);
        if (_dragMarker != Marker.None)
        {
            UpdateDrag(pos.X);
            e.Handled = true;
        }
        else
        {
            Cursor = HitTestMarker(pos.X) != Marker.None || pos.Y >= PlotBottom ? Cursors.SizeWE : null;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragMarker != Marker.None)
        {
            ReleaseMouseCapture(); // ends the drag via OnLostMouseCapture
            e.Handled = true;
        }
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        EndDrag();
    }

    private void UpdateDrag(double x)
    {
        double value = ToValue(x);
        var imageRange = new ValueRange(RangeMinimum, RangeMaximum);
        _dragValue = _dragMarker == Marker.Black
            ? HistogramMapping.ClampBlackPoint(value, imageRange, WhitePoint)
            : HistogramMapping.ClampWhitePoint(value, imageRange, BlackPoint);

        InvalidateVisual();

        if (!_commitPending)
        {
            _commitPending = true;
            CompositionTarget.Rendering += OnRendering;
        }
    }

    private void OnRendering(object? sender, EventArgs e) => CommitDragValue();

    private void CommitDragValue()
    {
        if (_commitPending)
        {
            _commitPending = false;
            CompositionTarget.Rendering -= OnRendering;
        }

        // SetCurrentValue keeps the two-way binding to the view model intact.
        if (_dragMarker == Marker.Black && BlackPoint != _dragValue)
            SetCurrentValue(BlackPointProperty, _dragValue);
        else if (_dragMarker == Marker.White && WhitePoint != _dragValue)
            SetCurrentValue(WhitePointProperty, _dragValue);
    }

    private void EndDrag()
    {
        if (_dragMarker == Marker.None)
            return;

        CommitDragValue();
        _dragMarker = Marker.None;
        InvalidateVisual();
    }

    #endregion
}
