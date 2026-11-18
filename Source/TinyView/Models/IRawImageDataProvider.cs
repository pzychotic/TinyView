namespace TinyView.Models;

public interface IRawImageDataProvider
{
    int Width { get; }
    int Height { get; }

    /// <summary>Minimum and maximum finite raw pixel value of the whole image ((0, 0) if there is none).</summary>
    ValueRange Range { get; }

    byte[] IndexedData { get; }

    /// <summary>
    /// Per-pixel mask of non-finite raw values (NaN, +Infinity, -Infinity): each byte is
    /// <see cref="OverlayPalette.NonFiniteIndex"/> for a non-finite pixel and
    /// <see cref="OverlayPalette.TransparentIndex"/> otherwise, so it can be used directly as
    /// <see cref="OverlayPalette"/>-indexed pixel data. Null when every value is finite.
    /// </summary>
    byte[]? NonFiniteMask { get; }

    string DataFormat { get; }
    string GetValueString(int x, int y);

    /// <summary>
    /// Re-normalizes the raw pixel data to <see cref="IndexedData"/> using
    /// the specified display range instead of the original <see cref="Range"/>.
    /// Values outside the range are clamped to 0 or 255.
    /// </summary>
    void RegenerateIndexedData(ValueRange displayRange);

    /// <summary>
    /// Computes the minimum and maximum finite raw pixel values within the
    /// specified rectangular region. Coordinates are clamped to the image bounds.
    /// NaN and ±Infinity are ignored; returns (0, 0) if the region has no finite value,
    /// and the global <see cref="Range"/> if the clamped region is empty.
    /// </summary>
    ValueRange GetRegionRange(int x, int y, int width, int height);

    /// <summary>
    /// Computes the distribution of the raw pixel values over <see cref="Range"/>
    /// using at most <paramref name="binCount"/> equally wide bins. Non-finite values are skipped.
    /// </summary>
    Histogram ComputeHistogram(int binCount);
}
