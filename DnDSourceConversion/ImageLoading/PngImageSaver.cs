using System.Drawing;
using System.Drawing.Imaging;

namespace DnDSourceConversion.ImageLoading;

public class PngImageSaver : IImageSaver
{
    public void Save(byte[] image, string path)
    {
        using var ms = new MemoryStream(image);
        using var img = Image.FromStream(ms);
        img.Save(path, ImageFormat.Png);
    }
}