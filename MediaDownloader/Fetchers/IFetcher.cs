using MediaDownloader.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDownloader.Fetchers;

public interface IFetcher
{
    public Task<List<MediaItem>> FetchMediaAsync(string username,CancellationToken cancel);
}
