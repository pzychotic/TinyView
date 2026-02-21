using BitMiracle.LibTiff.Classic;
using System.Runtime.InteropServices;
using TinyView.Models;

namespace TinyView.Services
{
    public class TiffImageLoader : IImageLoader
    {
        public bool CanLoad(string extension) => extension.Equals(".tif") || extension.Equals(".tiff");

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

                // Generic reader: read scanlines as TElement (blittable/unmanaged) and convert to TTarget (INumber)
                IRawImageDataProvider ReadAs<TElement, TTarget>(Func<TElement, TTarget> convert, string format)
                    where TElement : unmanaged
                    where TTarget : System.Numerics.INumber<TTarget>
                {
                    var pixelData = new TTarget[width, height];

                    for (int y = 0; y < height; ++y)
                    {
                        if (!tiff.ReadScanline(scanline, y))
                            throw new InvalidOperationException("Failed to read TIFF scanline.");

                        var values = MemoryMarshal.Cast<byte, TElement>(scanline.AsSpan());
                        for (int x = 0; x < width; ++x)
                        {
                            pixelData[x, y] = convert(values[x]);
                        }
                    }

                    return new RawImageData<TTarget>(width, height, pixelData, format);
                }

                if (isUInt16)
                {
                    return ReadAs<ushort, ushort>(v => (photometric == Photometric.MINISWHITE) ? (ushort)(ushort.MaxValue - v) : v, "Gray16 (ushort)");
                }

                if (isUInt32)
                {
                    return ReadAs<uint, uint>(v => (photometric == Photometric.MINISWHITE) ? uint.MaxValue - v : v, "Gray32 (uint)");
                }

                if (isFloat16)
                {
                    return ReadAs<Half, float>(v => (float)v, "Gray16 (half)");
                }

                // isFloat32
                return ReadAs<float, float>(v => v, "Gray32 (float)");
            });
    }
}
