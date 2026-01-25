using MediaDownloader.Models.X;
using Serilog.Events;

namespace MediaDownloader.Models;

public record CommandLineArguments(
    string Username,
    FileInfo CookieFile,
    string OutputDir,
    string OutputPathTemplate,
    MediaType DownloadType
);
