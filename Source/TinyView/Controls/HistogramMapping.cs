using TinyView.Models;

namespace TinyView.Controls;

/// <summary>
/// Pure math used by <see cref="HistogramControl"/>: mapping between raw values and
/// horizontal pixel positions, bar heights and marker clamping. Kept free of WPF types so it
/// can be unit-tested without rendering.
/// </summary>
internal static class HistogramMapping
{
    /// <summary>Maps <paramref name="value"/> in <paramref name="range"/> to [0, <paramref name="width"/>].</summary>
    public static double ValueToX(double value, ValueRange range, double width)
        => range.Normalize(value) * width;

    /// <summary>Inverse of <see cref="ValueToX"/>.</summary>
    public static double XToValue(double x, ValueRange range, double width)
    {
        if (width <= 0 || range.IsEmpty)
            return range.Min;
        return range.Lerp(x / width);
    }

    /// <summary>
    /// Bar height in [0, 1] for <paramref name="count"/> relative to <paramref name="maxCount"/>.
    /// Log scale uses log(1 + count) so single pixels stay visible next to huge spikes.
    /// </summary>
    public static double NormalizedHeight(int count, int maxCount, bool logScale)
    {
        if (count <= 0 || maxCount <= 0)
            return 0;
        return logScale
            ? Math.Log(1.0 + count) / Math.Log(1.0 + maxCount)
            : (double)count / maxCount;
    }

    /// <summary>
    /// Clamps a dragged black point to the image range and to at most <paramref name="whitePoint"/>.
    /// </summary>
    public static double ClampBlackPoint(double value, ValueRange imageRange, double whitePoint)
        => new ValueRange(imageRange.Min, Math.Min(imageRange.Max, whitePoint)).Clamp(value);

    /// <summary>
    /// Clamps a dragged white point to the image range and to at least <paramref name="blackPoint"/>.
    /// </summary>
    public static double ClampWhitePoint(double value, ValueRange imageRange, double blackPoint)
        => new ValueRange(Math.Max(imageRange.Min, blackPoint), imageRange.Max).Clamp(value);
}
