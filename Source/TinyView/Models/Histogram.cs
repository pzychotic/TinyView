namespace TinyView.Models;

/// <summary>
/// Pixel-value distribution of an image. Bin <c>i</c> covers the half-open value range
/// [<see cref="BinStart"/>(i), <see cref="BinStart"/>(i + 1)); the last bin also includes <see cref="Max"/>.
/// </summary>
/// <param name="Counts">Number of pixels per bin.</param>
/// <param name="Min">Lowest value covered by the first bin.</param>
/// <param name="Max">Highest value covered by the last bin.</param>
public sealed record Histogram(int[] Counts, double Min, double Max)
{
    /// <summary>Default bin count used for the histogram panel.</summary>
    public const int DefaultBinCount = 512;

    /// <summary>Largest count of any single bin (0 for an empty histogram).</summary>
    public int MaxCount { get; } = Counts.Length > 0 ? Counts.Max() : 0;

    /// <summary>Width of one bin in raw value units (0 when <see cref="Min"/> equals <see cref="Max"/>).</summary>
    public double BinWidth => Counts.Length > 0 ? (Max - Min) / Counts.Length : 0;

    /// <summary>Raw value at the lower edge of bin <paramref name="index"/>.</summary>
    public double BinStart(int index) => Min + index * BinWidth;
}
