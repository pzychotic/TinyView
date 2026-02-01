namespace TinyView.Models
{
    public interface IRawImageDataProvider
    {
        int Width { get; }
        int Height { get; }
        float Min { get; }
        float Max { get; }
        byte[] IndexedData { get; }
        string? DataFormat { get; }
        string? GetValueString(int x, int y);
    }
}
