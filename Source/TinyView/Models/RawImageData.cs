using System.Diagnostics;
using System.Numerics;

namespace TinyView.Models;

/// <summary>
/// Represents a two-dimensional array of raw image data and additional indexed data used to create a
/// WriteableBitmap from.
/// </summary>
/// <typeparam name="T">The type of the pixel data stored as the raw image data.</typeparam>
public sealed class RawImageData<T> : IRawImageDataProvider where T : INumber<T>
{
    private readonly T[] _rawData;
    private readonly string _dataFormat;

    public RawImageData(int width, int height, T[] data, string dataFormat)
    {
        Width = width;
        Height = height;

        (Min, Max) = ComputeMinMax(data);

        _rawData = data;
        IndexedData = new byte[Width * Height];

        _dataFormat = dataFormat;

        GenerateIndexedData(Min, Max);
    }

    /// <summary>
    /// Computes the minimum and maximum of <paramref name="data"/> in a single pass.
    /// Returns (0, 0) for an empty buffer.
    /// </summary>
    private static (double Min, double Max) ComputeMinMax(T[] data)
    {
        if (data.Length == 0)
            return (0, 0);

        T min = data[0];
        T max = data[0];
        for (int i = 1; i < data.Length; ++i)
        {
            T value = data[i];
            if (value < min) min = value;
            if (value > max) max = value;
        }

        return (double.CreateTruncating(min), double.CreateTruncating(max));
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
                double norm = (double.CreateTruncating(_rawData[offset + x]) - min) * scale;
                byte index = (byte)Math.Clamp(norm, 0, 255);
                IndexedData[offset + x] = index;
            }
        }
    }

    /// <inheritdoc />
    public void RegenerateIndexedData(double displayMin, double displayMax)
    {
        GenerateIndexedData(displayMin, displayMax);
    }

    /// <inheritdoc />
    public (double Min, double Max) GetRegionMinMax(int x, int y, int width, int height)
    {
        // Clamp the rectangle to the image bounds
        int x0 = Math.Max(0, x);
        int y0 = Math.Max(0, y);
        int x1 = Math.Min(Width, x + width);
        int y1 = Math.Min(Height, y + height);

        double min = double.MaxValue;
        double max = double.MinValue;

        for (int row = y0; row < y1; row++)
        {
            int offset = row * Width;
            for (int col = x0; col < x1; col++)
            {
                double val = double.CreateTruncating(_rawData[offset + col]);
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

    public double Min { get; }
    public double Max { get; }

    public byte[] IndexedData { get; }

    public string DataFormat => _dataFormat;

    public string GetValueString(int x, int y)
    {
        Debug.Assert(x >= 0 && x < Width && y >= 0 && y < Height);
        return GetValueStringPrecise(_rawData[y * Width + x]);
    }

    private string GetValueStringPrecise(T value)
    {
        return value switch
        {
            // correctly display Half values, see: https://github.com/dotnet/runtime/issues/75204
            Half h => ((float)h).ToString(),
            _ => value.ToString() ?? string.Empty
        };
    }
}
