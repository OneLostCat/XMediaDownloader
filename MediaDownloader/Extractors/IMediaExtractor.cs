using MediaDownloader.Models;

namespace MediaDownloader.Extractors;

public interface IMediaExtractor
{
    public Task<List<MediaInfo>> ExtractAsync(CancellationToken cancel);
}
