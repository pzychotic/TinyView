using TinyView.Models;

namespace TinyView.Services
{
    public interface IImageLoader
    {
        bool CanLoad(string extension);
        Task<IRawImageDataProvider> LoadImageAsync(string path);
    }
}
