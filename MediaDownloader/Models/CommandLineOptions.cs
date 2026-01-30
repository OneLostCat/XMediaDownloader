namespace MediaDownloader.Models;

public record CommandLineOptions(
    MediaExtractor Extractor,
    string User,
    string Cookie,
    string Output,
    string? OutputTemplate,
    string DateTimeFormat,
    List<MediaType> Type,
    int Concurrency
);
