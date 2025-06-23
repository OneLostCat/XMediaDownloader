namespace XMediaDownloader.Models.XApi;

public record GraphQlResponse<T>
{
    public T? Data { get; set; }
    public GraphQlResponseErrors? Errors { get; set; }
}

public record GraphQlResponseErrors
{
    public required string Message { get; set; }
    public List<GraphQlResponseErrorLocation> Locations { get; set; } = [];
}

public record GraphQlResponseErrorLocation
{
    public required int Line { get; set; }
    public required int Column { get; set; }
}
