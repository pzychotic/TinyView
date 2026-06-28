using BitMiracle.LibTiff.Classic;
using System.Runtime.InteropServices;
using TinyView.Models;

namespace TinyView.Services;

public sealed class TiffImageLoader : IImageLoader
{
    public bool CanLoad(string extension) => extension.Equals(".tif", StringComparison.OrdinalIgnoreCase) || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase);

    public Task<IRawImageDataProvider> LoadImageAsync(string path)
        => Task.Run<IRawImageDataProvider>(() =>
        {
            using var tiff = Tiff.Open(path, "r")
                ?? throw new InvalidOperationException("Unable to open TIFF image.");

            static int GetRequiredInt(Tiff tiff, TiffTag tag)
            {
                var field = tiff.GetField(tag)
                    ?? throw new InvalidOperationException($"Missing required TIFF tag '{tag}'.");
                return field[0].ToInt();
            }

            static short GetShortOrDefault(Tiff tiff, TiffTag tag, short defaultValue)
                => tiff.GetField(tag) is { } field ? field[0].ToShort() : defaultValue;

            int width = GetRequiredInt(tiff, TiffTag.IMAGEWIDTH);
            int height = GetRequiredInt(tiff, TiffTag.IMAGELENGTH);

            var samplesPerPixel = GetShortOrDefault(tiff, TiffTag.SAMPLESPERPIXEL, 1);
            var bitsPerSample = GetShortOrDefault(tiff, TiffTag.BITSPERSAMPLE, 1);
            var sampleFormat = (SampleFormat)GetShortOrDefault(tiff, TiffTag.SAMPLEFORMAT, (short)SampleFormat.UINT);
            var photometric = (Photometric)GetShortOrDefault(tiff, TiffTag.PHOTOMETRIC, (short)Photometric.MINISBLACK);

            if (samplesPerPixel != 1)
                throw new InvalidOperationException("Expected a single-channel grayscale image.");

            bool isUInt16 = bitsPerSample == 16 && sampleFormat == SampleFormat.UINT;
            bool isUInt32 = bitsPerSample == 32 && sampleFormat == SampleFormat.UINT;
            bool isFloat16 = bitsPerSample == 16 && sampleFormat == SampleFormat.IEEEFP;
            bool isFloat32 = bitsPerSample == 32 && sampleFormat == SampleFormat.IEEEFP;

            if (!isUInt16 && !isUInt32 && !isFloat16 && !isFloat32)
                throw new InvalidOperationException("Expected a 16/32-bit grayscale TIFF (uint or float).");

            if (photometric != Photometric.MINISBLACK && photometric != Photometric.MINISWHITE)
                throw new InvalidOperationException("Expected a grayscale TIFF (Photometric MINISBLACK/MINISWHITE).");

            int scanlineSize = tiff.ScanlineSize();
            int bytesPerPixel = bitsPerSample / 8;
            int expectedBytesPerScanline = width * bytesPerPixel;
            if (scanlineSize < expectedBytesPerScanline || scanlineSize % bytesPerPixel != 0)
                throw new InvalidOperationException("Unsupported TIFF scanline layout.");

            var scanline = new byte[scanlineSize];

            // Generic reader: read scanlines as T (blittable/unmanaged and INumber).
            // When convert is null the scanline is bulk-copied; otherwise convert is applied per pixel.
            IRawImageDataProvider ReadAs<T>(string format, Func<T, T>? convert = null)
                where T : unmanaged, System.Numerics.INumber<T>
            {
                var pixelData = new T[width * height];

                for (int y = 0; y < height; ++y)
                {
                    if (!tiff.ReadScanline(scanline, y))
                        throw new InvalidOperationException("Failed to read TIFF scanline.");

                    var src = MemoryMarshal.Cast<byte, T>(scanline.AsSpan(0, expectedBytesPerScanline));
                    var dst = pixelData.AsSpan(y * width, width);

                    if (convert is null)
                        src.CopyTo(dst);
                    else
                        for (int x = 0; x < width; ++x)
                            dst[x] = convert(src[x]);
                }

                return new RawImageData<T>(width, height, pixelData, format);
            }

            if (isUInt16)
                return ReadAs<ushort>("Gray16 (ushort)", photometric == Photometric.MINISWHITE ? v => (ushort)(ushort.MaxValue - v) : null);

            if (isUInt32)
                return ReadAs<uint>("Gray32 (uint)", photometric == Photometric.MINISWHITE ? v => uint.MaxValue - v : null);

            if (isFloat16)
                return ReadAs<Half>("Gray16 (half)");

            // isFloat32
            return ReadAs<float>("Gray32 (float)");
        });
}
