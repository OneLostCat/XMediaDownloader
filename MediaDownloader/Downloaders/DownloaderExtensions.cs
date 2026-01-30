using Microsoft.Extensions.DependencyInjection;

namespace MediaDownloader.Downloaders;

public static class DownloaderExtensions
{
    public static IServiceCollection AddDownloaders(this IServiceCollection services)
    {
        services.AddSingleton<HttpDownloader>();
        services.AddSingleton<JustForFansDownloader>();
        
        return services;
    }
}
