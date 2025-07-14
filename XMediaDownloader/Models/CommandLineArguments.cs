using Serilog.Events;

namespace XMediaDownloader.Models;

public record CommandLineArguments(
    string Username,
    string CookieFile,
    string OutputDir,
    string OutputPathFormat,
    MediaType DownloadType,
    bool WithoutDownloadInfo,
    bool WithoutDownloadMedia,
    string WorkDir,
    LogEventLevel LogLevel
);
