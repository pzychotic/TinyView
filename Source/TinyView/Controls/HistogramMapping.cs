namespace TinyView.Controls;

/// <summary>
/// Pure math used by <see cref="HistogramControl"/>: mapping between raw values and
/// horizontal pixel positions, bar heights and marker clamping. Kept free of WPF types so it
/// can be unit-tested without rendering.
/// </summary>
internal static class HistogramMapping
{
    /// <summary>Maps <paramref name="value"/> in [<paramref name="min"/>, <paramref name="max"/>] to [0, <paramref name="width"/>].</summary>
    public static double ValueToX(double value, double min, double max, double width)
    {
        if (max <= min)
            return width / 2;
        return (value - min) / (max - min) * width;
    }

    /// <summary>Inverse of <see cref="ValueToX"/>.</summary>
    public static double XToValue(double x, double min, double max, double width)
    {
        if (width <= 0 || max <= min)
            return min;
        return min + x / width * (max - min);
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
    public static double ClampBlackPoint(double value, double imageMin, double imageMax, double whitePoint)
        => Clamp(value, imageMin, Math.Min(imageMax, whitePoint));

    /// <summary>
    /// Clamps a dragged white point to the image range and to at least <paramref name="blackPoint"/>.
    /// </summary>
    public static double ClampWhitePoint(double value, double imageMin, double imageMax, double blackPoint)
        => Clamp(value, Math.Max(imageMin, blackPoint), imageMax);

    // Unlike Math.Clamp this does not throw when lower > upper (e.g. a point typed outside the
    // image range); the lower bound wins in that case.
    private static double Clamp(double value, double lower, double upper)
        => Math.Max(lower, Math.Min(value, upper));
}
