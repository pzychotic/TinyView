namespace TinyView.Models;

public interface IRawImageDataProvider
{
    int Width { get; }
    int Height { get; }
    double Min { get; }
    double Max { get; }
    byte[] IndexedData { get; }
    string DataFormat { get; }
    string GetValueString(int x, int y);

    /// <summary>
    /// Re-normalizes the raw pixel data to <see cref="IndexedData"/> using
    /// the specified display range instead of the original <see cref="Min"/>/<see cref="Max"/>.
    /// Values outside the range are clamped to 0 or 255.
    /// </summary>
    void RegenerateIndexedData(double displayMin, double displayMax);

    /// <summary>
    /// Computes the minimum and maximum raw pixel values within the
    /// specified rectangular region. Coordinates are clamped to the image bounds.
    /// </summary>
    (double Min, double Max) GetRegionMinMax(int x, int y, int width, int height);
}
