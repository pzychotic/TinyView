namespace TinyView.Models;

/// <summary>
/// Pixel-value distribution of an image. Bin <c>i</c> covers the half-open value range
/// [<see cref="BinStart"/>(i), <see cref="BinStart"/>(i + 1)); the last bin also includes <see cref="ValueRange.Max"/>.
/// </summary>
/// <param name="Counts">Number of pixels per bin.</param>
/// <param name="Range">Value range covered by the bins, from the lower edge of the first bin to the upper edge of the last.</param>
/// <param name="NaNCount">Number of NaN pixels (not included in <paramref name="Counts"/>).</param>
/// <param name="InfinityCount">Number of +Infinity and -Infinity pixels (not included in <paramref name="Counts"/>).</param>
public sealed record Histogram(int[] Counts, ValueRange Range, int NaNCount = 0, int InfinityCount = 0)
{
    /// <summary>True when the image contains NaN or ±Infinity pixels.</summary>
    public bool HasNonFiniteValues => NaNCount > 0 || InfinityCount > 0;

    /// <summary>Default bin count used for the histogram panel.</summary>
    public const int DefaultBinCount = 512;

    /// <summary>Largest count of any single bin (0 for an empty histogram).</summary>
    public int MaxCount { get; } = Counts.Length > 0 ? Counts.Max() : 0;

    /// <summary>Width of one bin in raw value units (0 for a zero-width <see cref="Range"/>).</summary>
    public double BinWidth => Counts.Length > 0 ? Range.Length / Counts.Length : 0;

    /// <summary>Raw value at the lower edge of bin <paramref name="index"/>.</summary>
    public double BinStart(int index) => Range.Min + index * BinWidth;
}
