using MediaDownloader.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDownloader.Fetchers;

public class JustForFansFetcher: IFetcher
{
    public Task<List<MediaItem>> FetchMediaAsync(string username, CancellationToken cancel)
    {
        throw new NotImplementedException();
    }
}
