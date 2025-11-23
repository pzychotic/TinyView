using System.Diagnostics;

namespace TinyView
{
    public interface IRawImageDataProvider
    {
        string? GetValueString(int x, int y);
    }

    public class RawImageData<T> : IRawImageDataProvider
    {
        public RawImageData( int width, int height, T[,] data)
        {
            Width = width;
            Height = height;

            _rawData = data;
        }

        public readonly int Width;
        public readonly int Height;

        private T[,] _rawData;

        public string? GetValueString(int x, int y)
        {
            Debug.Assert(x >= 0 && x < Width && y >= 0 && y < Height);
            return _rawData[x, y]?.ToString();
        }
    }
}
