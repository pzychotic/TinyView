using System.Runtime.InteropServices;
using TinyEXR.V3;
using TinyView.Models;

namespace TinyView.Services;

public sealed class TinyExrImageLoader : IImageLoader
{
    public string Description => "EXR Files";

    public IReadOnlyList<string> Extensions { get; } = [".exr"];

    public Task<IRawImageDataProvider> LoadImageAsync(string path)
        => Task.Run<IRawImageDataProvider>(() =>
        {
            ReaderResult<Image> result = ExrFile.LoadFromFile(path);
            if (!result.IsSuccess || result.Value is not Image image)
                throw new InvalidOperationException($"Failed to load EXR image: {result.Status}", result.Error);

            Part part = image.Parts[0];
            if (part.Header.IsDeep)
                throw new InvalidOperationException("Deep EXR images are not supported.");

            PartLevel level = part.GetLevel(0, 0);
            if (level.Channels.Count != 1)
                throw new InvalidOperationException("Expected a single channel image.");

            ChannelBuffer channel = level.Channels[0];
            string channelName = channel.Name;

            int width = checked((int)level.Width);
            int height = checked((int)level.Height);

            switch (channel.PixelType)
            {
                case PixelType.Float:
                    var floatData = MemoryMarshal.Cast<byte, float>(channel.Data).ToArray();
                    return new RawImageData<float>(width, height, floatData, $"{channelName} (float)");
                case PixelType.Half:
                    var halfData = MemoryMarshal.Cast<byte, Half>(channel.Data).ToArray();
                    return new RawImageData<Half>(width, height, halfData, $"{channelName} (half)");
                case PixelType.UInt:
                    var uintData = MemoryMarshal.Cast<byte, uint>(channel.Data).ToArray();
                    return new RawImageData<uint>(width, height, uintData, $"{channelName} (uint)");
                default:
                    throw new InvalidOperationException("Unsupported pixel type.");
            }
        });
}
