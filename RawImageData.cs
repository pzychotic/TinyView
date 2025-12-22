using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace TinyView
{
    public interface IRawImageDataProvider
    {
        int Width { get; }
        int Height { get; }
        byte[] IndexedData { get; }
        string? DataFormat { get; }
        string? GetValueString(int x, int y);
    }

    /// <summary>
    /// Represents a two-dimensional array of raw image data and additional indexed data used to create a
    /// WriteableBitmap from.
    /// </summary>
    /// <typeparam name="T">The type of the pixel data stored as the raw image data.</typeparam>
    public class RawImageData<T> : IRawImageDataProvider where T : INumber<T>
    {
        private readonly string _dataFormat;

        public RawImageData(int width, int height, T[,] data, string dataFormat)
        {
            Width = width;
            Height = height;

            Min = Convert.ToSingle(data.Cast<T>().Min());
            Max = Convert.ToSingle(data.Cast<T>().Max());

            _rawData = data;
            IndexedData = new byte[Width * Height];

            _dataFormat = dataFormat;

            GenerateIndexedData();
        }

        /// <summary>
        /// Generates indexed data by normalizing the raw data values between min and max to a byte range.
        /// </summary>
        private void GenerateIndexedData()
        {
            float scale = Min == Max ? 1f : 255f / (Max - Min);
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    float norm = (Convert.ToSingle(_rawData[x, y]) - Min) * scale;
                    byte index = (byte)Math.Clamp(norm, 0, 255);
                    IndexedData[y * Width + x] = index;
                }
            }
        }

        public int Width { get; }
        public int Height { get; }


        public byte[] IndexedData { get; }

        public string? DataFormat => _dataFormat;

        public string? GetValueString(int x, int y)
        {
            Debug.Assert(x >= 0 && x < Width && y >= 0 && y < Height);
            return _rawData[x, y].ToString();
        }

        private T[,] _rawData;

        private readonly float Min;
        private readonly float Max;
    }
}
