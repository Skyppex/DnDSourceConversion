namespace DnDSourceConversion.ImageLoading;

public interface IImageSaver
{
    void Save(byte[] imageBytes, string path);
}