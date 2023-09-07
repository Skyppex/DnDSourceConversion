using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace DnDSourceConversion.ImageLoading;

public sealed class ImageDownloader : IDisposable
{
    private readonly WebClient _client;
    
    public ImageDownloader() => _client = new WebClient();

    [SuppressMessage("ReSharper.DPA", "DPA0003: Excessive memory allocations in LOH", MessageId = "type: System.Byte[]; size: 271MB")]
    public byte[]? DownloadImage(Uri apiAddress)
    {
        byte[] imageData;
        
        try
        {
            imageData = _client.DownloadData(apiAddress);
        }
        catch (WebException ex)
        {
            return null;
            
            // if (ex is not { Status: WebExceptionStatus.ProtocolError, Response: not null })
            //     return null;
            //
            //
            // var resp = (HttpWebResponse) ex.Response;
            //
            // if (resp.StatusCode == HttpStatusCode.NotFound)
            //     return null;
            // else
            //     return null;
        }
        
        return imageData;
    }

    public async Task<byte[]> DownloadImageAsync(Uri apiAddress)
    {
        byte[] imageData;

        try
        {
            imageData = await _client.DownloadDataTaskAsync(apiAddress);
        }
        catch (WebException ex)
        {
            return null;
            
            // if (ex is not { Status: WebExceptionStatus.ProtocolError, Response: not null })
            //     return null;
            //
            //
            // var resp = (HttpWebResponse) ex.Response;
            //
            // if (resp.StatusCode == HttpStatusCode.NotFound)
            //     return null;
            // else
            //     return null;
        }

        return imageData;
    }

    public void Dispose() => _client.Dispose();
}