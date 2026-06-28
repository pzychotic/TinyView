using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using TinyEXR;
using TinyView.Models;

namespace TinyView.Services;

public sealed class TinyExrImageLoader : IImageLoader
{
    public bool CanLoad(string extension) => extension.Equals(".exr", StringComparison.OrdinalIgnoreCase);

    public Task<IRawImageDataProvider> LoadImageAsync(string path)
        => Task.Run<IRawImageDataProvider>(() =>
        {
            ResultCode versionResult = Exr.ParseEXRVersionFromFile(path, out ExrVersion version);
            if (versionResult != ResultCode.Success)
                throw new InvalidOperationException($"Failed to parse EXR version: {versionResult}");

            ResultCode headerResult = Exr.ParseEXRHeaderFromFile(path, out _, out ExrHeader header);
            if (headerResult != ResultCode.Success)
                throw new InvalidOperationException($"Failed to parse EXR header: {headerResult}");

            ResultCode imageResult = Exr.LoadEXRImageFromFile(path, header, out ExrImage image);
            if (imageResult != ResultCode.Success)
                throw new InvalidOperationException($"Failed to load EXR image: {imageResult}");

            if (image.Channels.Count != 1)
                throw new InvalidOperationException("Expected a single channel image.");

            ExrImageChannel channel = image.Channels[0];
            string channelName = channel.Channel.Name;

            int width = image.Width;
            int height = image.Height;

            switch (channel.DataType)
            {
                case ExrPixelType.Float:
                    var floatData = MemoryMarshal.Cast<byte, float>(channel.Data).ToArray();
                    return new RawImageData<float>(width, height, floatData, $"{channelName} (float)");
                case ExrPixelType.Half:
                    var halfData = MemoryMarshal.Cast<byte, Half>(channel.Data).ToArray();
                    return new RawImageData<Half>(width, height, halfData, $"{channelName} (half)");
                case ExrPixelType.UInt:
                    var uintData = MemoryMarshal.Cast<byte, uint>(channel.Data).ToArray();
                    return new RawImageData<uint>(width, height, uintData, $"{channelName} (uint)");
                default:
                    throw new InvalidOperationException("Unsupported pixel type.");
            }
        });
}
