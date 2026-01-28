namespace MediaDownloader.Models;

public record CommandLineOptions(
    MediaExtractor Extractor,
    string User,
    string Cookie,
    string Output,
    string? OutputTemplate,
    List<MediaType> Type
);
