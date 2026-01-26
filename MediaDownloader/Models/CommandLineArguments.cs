using MediaDownloader.Models.X;
using Serilog.Events;

namespace MediaDownloader.Models;

public record CommandLineArguments(
    MediaExtractor Extractor,
    string Username,
    FileInfo Cookie,
    string Output,
    string? OutputTemplate,
    XMediaType Type);
