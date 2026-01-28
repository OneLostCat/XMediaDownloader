namespace MediaDownloader.Models.JustForFans.Api;

public record PlaylistBody(
    string Action,
    string UserHash,
    string Title,
    string Description,
    string MovieHash,
    string AccessControl,
    string ExistingPlaylistId
);
