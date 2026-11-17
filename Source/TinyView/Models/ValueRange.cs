namespace TinyView.Models;

/// <summary>
/// A closed range [<see cref="Min"/>, <see cref="Max"/>] of raw pixel values, e.g. the value range of
/// an image or the display range used for normalization.
/// </summary>
/// <remarks>
/// <see cref="Min"/> &gt; <see cref="Max"/> is deliberately allowed (the user can type an inverted
/// display range); such a range counts as <see cref="IsEmpty"/>.
/// </remarks>
public readonly record struct ValueRange(double Min, double Max)
{
    /// <summary>Signed width of the range (<see cref="Max"/> - <see cref="Min"/>).</summary>
    public double Length => Max - Min;

    /// <summary>True when the range has no positive width (zero-width or inverted).</summary>
    public bool IsEmpty => !(Max > Min);

    /// <summary>
    /// Maps <paramref name="value"/> to its relative position in the range (0 at <see cref="Min"/>,
    /// 1 at <see cref="Max"/>); values outside the range map outside [0, 1]. Returns 0.5 for an
    /// <see cref="IsEmpty"/> range.
    /// </summary>
    public double Normalize(double value) => IsEmpty ? 0.5 : (value - Min) / Length;

    /// <summary>Inverse of <see cref="Normalize"/>: the value at relative position <paramref name="t"/>.</summary>
    public double Lerp(double t) => Min + t * Length;

    /// <summary>
    /// Clamps <paramref name="value"/> to the range. Unlike <see cref="Math.Clamp(double, double, double)"/>
    /// this does not throw for an inverted range; <see cref="Min"/> wins in that case.
    /// </summary>
    public double Clamp(double value) => Math.Max(Min, Math.Min(value, Max));

    public override string ToString() => $"{Min:0.##}..{Max:0.##}";
}
