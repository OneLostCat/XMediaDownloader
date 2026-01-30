using MediaDownloader.Models;

namespace MediaDownloader.Downloaders;

public interface IMediaDownloader
{
    public Task DownloadAsync(List<MediaInfo> medias, CancellationToken cancel);
}
