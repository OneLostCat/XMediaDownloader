using MediaDownloader.Models;

namespace MediaDownloader.Extractors;

public interface IMediaExtractor
{
    public Task<MediaCollection> ExtractAsync(CancellationToken cancel);
}
