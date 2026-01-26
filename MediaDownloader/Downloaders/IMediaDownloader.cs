using MediaDownloader.Models;

namespace MediaDownloader.Downloaders;

public interface IMediaDownloader
{
    public Task DownloadAsync(MediaCollection medias, CancellationToken cancel);
}
