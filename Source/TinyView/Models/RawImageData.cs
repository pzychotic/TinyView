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

        Range = ComputeRange(data);
        NonFiniteMask = ComputeNonFiniteMask(data);

        _rawData = data;
        IndexedData = new byte[Width * Height];

        _dataFormat = dataFormat;

        GenerateIndexedData(Range);
    }

    /// <summary>
    /// Computes the minimum and maximum of the finite values in <paramref name="data"/> in a single pass.
    /// NaN and ±Infinity are ignored. Returns (0, 0) when there is no finite value (e.g. an empty buffer).
    /// </summary>
    private static ValueRange ComputeRange(ReadOnlySpan<T> data)
    {
        T min = T.Zero;
        T max = T.Zero;
        bool found = false;
        for (int i = 0; i < data.Length; ++i)
        {
            T value = data[i];
            if (!T.IsFinite(value))
                continue;

            if (!found)
            {
                min = max = value;
                found = true;
            }
            else if (value < min) min = value;
            else if (value > max) max = value;
        }

        return new ValueRange(double.CreateTruncating(min), double.CreateTruncating(max));
    }

    /// <summary>
    /// Marks every NaN and ±Infinity value in <paramref name="data"/> with
    /// <see cref="OverlayPalette.NonFiniteIndex"/>. Returns null when all values are finite,
    /// so images without such values (including all integer images) need no mask buffer.
    /// </summary>
    private static byte[]? ComputeNonFiniteMask(ReadOnlySpan<T> data)
    {
        byte[]? mask = null;
        for (int i = 0; i < data.Length; ++i)
        {
            if (T.IsFinite(data[i]))
                continue;

            mask ??= new byte[data.Length];
            mask[i] = OverlayPalette.NonFiniteIndex;
        }

        return mask;
    }

    /// <summary>
    /// Generates indexed data by normalizing the raw data values within
    /// <paramref name="range"/> to a 0-255 byte range. Values outside the range are clamped.
    /// An inverted range (Min &gt; Max) intentionally inverts the mapping.
    /// </summary>
    private void GenerateIndexedData(ValueRange range)
    {
        double min = range.Min;
        double scale = range.Length == 0 ? 1.0 : 255.0 / range.Length;
        ReadOnlySpan<T> rawSpan = _rawData;
        Span<byte> indexedSpan = IndexedData;
        for (int i = 0; i < rawSpan.Length; ++i)
        {
            double norm = (double.CreateTruncating(rawSpan[i]) - min) * scale;
            indexedSpan[i] = (byte)Math.Clamp(norm, 0, 255);
        }
    }

    /// <inheritdoc />
    public void RegenerateIndexedData(ValueRange displayRange)
    {
        GenerateIndexedData(displayRange);
    }

    /// <inheritdoc />
    public ValueRange GetRegionRange(int x, int y, int width, int height)
    {
        // Clamp the rectangle to the image bounds
        int x0 = Math.Max(0, x);
        int y0 = Math.Max(0, y);
        int x1 = Math.Min(Width, x + width);
        int y1 = Math.Min(Height, y + height);

        // If the region is empty, fall back to the global range
        if (x1 <= x0 || y1 <= y0)
            return Range;

        double min = double.MaxValue;
        double max = double.MinValue;

        ReadOnlySpan<T> rawSpan = _rawData;
        int colCount = x1 - x0;
        for (int row = y0; row < y1; row++)
        {
            ReadOnlySpan<T> rowSlice = rawSpan.Slice(row * Width + x0, colCount);
            for (int i = 0; i < colCount; i++)
            {
                double val = double.CreateTruncating(rowSlice[i]);
                if (!double.IsFinite(val))
                    continue;
                if (val < min) min = val;
                if (val > max) max = val;
            }
        }

        // The region contains no finite value
        if (min > max)
            return default;

        return new ValueRange(min, max);
    }

    // True for integral pixel types: CreateTruncating(0.5) rounds to zero, which is an integer.
    private static readonly bool IsIntegerType = T.IsInteger(T.CreateTruncating(0.5));

    /// <inheritdoc />
    public Histogram ComputeHistogram(int binCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(binCount, 1);

        double length = Range.Length;
        ValueRange histRange = Range;

        if (length == 0 || (IsIntegerType && length < binCount))
        {
            // One bin per distinct integer value (or a single bin for a constant image),
            // centered on the value so the markers line up with the bars.
            binCount = (int)length + 1;
            histRange = new ValueRange(Range.Min - 0.5, Range.Max + 0.5);
        }

        var counts = new int[binCount];
        double histMin = histRange.Min;
        double scale = binCount / histRange.Length;
        int lastBin = binCount - 1;

        int nanCount = 0;
        int infinityCount = 0;

        ReadOnlySpan<T> rawSpan = _rawData;
        for (int i = 0; i < rawSpan.Length; ++i)
        {
            double value = double.CreateTruncating(rawSpan[i]);
            if (!double.IsFinite(value))
            {
                if (double.IsNaN(value)) nanCount++;
                else infinityCount++;
                continue;
            }

            int bin = (int)((value - histMin) * scale);
            counts[Math.Clamp(bin, 0, lastBin)]++;
        }

        return new Histogram(counts, histRange, nanCount, infinityCount);
    }

    public int Width { get; }
    public int Height { get; }

    public ValueRange Range { get; }

    public byte[] IndexedData { get; }

    /// <inheritdoc />
    public byte[]? NonFiniteMask { get; }

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
