using TinyView.Models;

namespace TinyView.Services;

public interface IImageLoader
{
    /// <summary>
    /// Human-readable format name used in file dialog filters, e.g. "PNG Files".
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Supported file extensions including the leading dot, e.g. [".tif", ".tiff"].
    /// </summary>
    IReadOnlyList<string> Extensions { get; }

    bool CanLoad(string extension) => Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);

    Task<IRawImageDataProvider> LoadImageAsync(string path);
}
