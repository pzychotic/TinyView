namespace TinyView.Models;

public interface IRawImageDataProvider
{
    int Width { get; }
    int Height { get; }

    /// <summary>Minimum and maximum raw pixel value of the whole image.</summary>
    ValueRange Range { get; }

    byte[] IndexedData { get; }
    string DataFormat { get; }
    string GetValueString(int x, int y);

    /// <summary>
    /// Re-normalizes the raw pixel data to <see cref="IndexedData"/> using
    /// the specified display range instead of the original <see cref="Range"/>.
    /// Values outside the range are clamped to 0 or 255.
    /// </summary>
    void RegenerateIndexedData(ValueRange displayRange);

    /// <summary>
    /// Computes the minimum and maximum raw pixel values within the
    /// specified rectangular region. Coordinates are clamped to the image bounds.
    /// </summary>
    ValueRange GetRegionRange(int x, int y, int width, int height);

    /// <summary>
    /// Computes the distribution of the raw pixel values over <see cref="Range"/>
    /// using at most <paramref name="binCount"/> equally wide bins. Non-finite values are skipped.
    /// </summary>
    Histogram ComputeHistogram(int binCount);
}
