namespace XMediaDownloader.Models;

public record CommandLineArguments
{
    public required string Username { get; init; }
    public required FileInfo CookieFile { get; init; }
    public required string OutputPath { get; init; }
    public required MediaType MediaType { get; init; }
    public required bool WithoutDownloadInfo { get; init; }
    public required bool WithoutDownloadMedia { get; init; }
}
