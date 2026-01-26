using MediaDownloader.Models;

namespace MediaDownloader.Extractors;

public class JustForFansExtractor: IMediaExtractor
{
    public Task<MediaCollection> ExtractAsync(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }
}
