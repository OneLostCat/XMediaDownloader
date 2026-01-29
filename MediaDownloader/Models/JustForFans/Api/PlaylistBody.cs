using System.Text.Json.Serialization;

namespace MediaDownloader.Models.JustForFans.Api;

public record PlaylistBody
{
    public required string Action { get; set; }
    public required string UserHash { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public string MovieHash { get; set; } = "";
    public string AccessControl { get; set; } = "Private";
    [JsonPropertyName("ExistingPlaylistID")] public string ExistingPlaylistId { get; set; } = "0";
}
