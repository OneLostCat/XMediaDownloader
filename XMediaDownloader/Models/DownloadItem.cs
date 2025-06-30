namespace XMediaDownloader.Models;

public record DownloadItem(string Url, string Extension, int? Bitrate = null);
