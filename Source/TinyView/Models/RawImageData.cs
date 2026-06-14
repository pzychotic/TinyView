using System.Diagnostics;
using System.Numerics;

namespace TinyView.Models;

/// <summary>
/// Represents a two-dimensional array of raw image data and additional indexed data used to create a
/// WriteableBitmap from.
/// </summary>
/// <typeparam name="T">The type of the pixel data stored as the raw image data.</typeparam>
public class RawImageData<T> : IRawImageDataProvider where T : INumber<T>
{
    private readonly string _dataFormat;

    public RawImageData(int width, int height, T[] data, string dataFormat)
    {
        Width = width;
        Height = height;

        Min = Convert.ToSingle(data.Min());
        Max = Convert.ToSingle(data.Max());

        _rawData = data;
        IndexedData = new byte[Width * Height];

        _dataFormat = dataFormat;

        GenerateIndexedData(Min, Max);
    }

    /// <summary>
    /// Generates indexed data by normalizing the raw data values between
    /// <paramref name="min"/> and <paramref name="max"/> to a 0-255 byte range.
    /// Values outside the range are clamped.
    /// </summary>
    private void GenerateIndexedData(double min, double max)
    {
        double scale = min == max ? 1.0 : 255.0 / (max - min);
        for (int y = 0; y < Height; ++y)
        {
            int offset = y * Width;
            for (int x = 0; x < Width; ++x)
            {
                double norm = (Convert.ToDouble(_rawData[offset + x]) - min) * scale;
                byte index = (byte)Math.Clamp(norm, 0, 255);
                IndexedData[offset + x] = index;
            }
        }
    }

    /// <inheritdoc />
    public void RegenerateIndexedData(float displayMin, float displayMax)
    {
        GenerateIndexedData(displayMin, displayMax);
    }

    /// <inheritdoc />
    public (float Min, float Max) GetRegionMinMax(int x, int y, int width, int height)
    {
        // Clamp the rectangle to the image bounds
        int x0 = Math.Max(0, x);
        int y0 = Math.Max(0, y);
        int x1 = Math.Min(Width, x + width);
        int y1 = Math.Min(Height, y + height);

        float min = float.MaxValue;
        float max = float.MinValue;

        for (int row = y0; row < y1; row++)
        {
            int offset = row * Width;
            for (int col = x0; col < x1; col++)
            {
                float val = Convert.ToSingle(_rawData[offset + col]);
                if (val < min) min = val;
                if (val > max) max = val;
            }
        }

        // If the region was empty, fall back to global min/max
        if (min > max)
            return (Min, Max);

        return (min, max);
    }

    public int Width { get; }
    public int Height { get; }

    public float Min { get; }
    public float Max { get; }

    public byte[] IndexedData { get; }

    public string? DataFormat => _dataFormat;

    public string? GetValueString(int x, int y)
    {
        Debug.Assert(x >= 0 && x < Width && y >= 0 && y < Height);
        return _rawData[y * Width + x].ToString();
    }

    private readonly T[] _rawData;
}
