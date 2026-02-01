using TinyView.Models;

namespace TinyView.Services
{
    public interface IImageLoader
    {
        bool CanLoad(string extension);
        IRawImageDataProvider LoadImage(string path);
    }
}
