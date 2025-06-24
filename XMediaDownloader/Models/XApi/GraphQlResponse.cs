namespace XMediaDownloader.Models.XApi;

public record GraphQlResponse<T>(T? Data, GraphQlResponseErrors? Errors);

public record GraphQlResponseErrors(string Message, List<GraphQlResponseErrorLocation> Locations);

public record GraphQlResponseErrorLocation(int Line, int Column);
