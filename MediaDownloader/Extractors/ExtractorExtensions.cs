using Microsoft.Extensions.DependencyInjection;

namespace MediaDownloader.Extractors;

public static class ExtractorExtensions
{
    public static IServiceCollection AddExtractors(this IServiceCollection services)
    {
        services.AddSingleton<XExtractor>();
        services.AddSingleton<JustForFansExtractor>();
        
        return services;
    }
}
