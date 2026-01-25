namespace MediaDownloader.Models.X.Api;

public record GraphQlResponse<T>
{
    public T? Data { get; set; }
    public List<GraphQlResponseErrors> Errors { get; set; } = [];
}

public record GraphQlResponseErrors
{
    public required string Message { get; set; }
    public required List<GraphQlResponseErrorLocation> Locations { get; set; }
}

public record GraphQlResponseErrorLocation
{
    public required int Line { get; set; }
    public required int Column { get; set; }
}
