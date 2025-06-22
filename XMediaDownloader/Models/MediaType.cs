namespace XMediaDownloader.Models;

[Flags]
public enum MediaType
{
    Image = 0b001,
    Video = 0b010,
    Gif = 0b100,
    All = 0b111,
}
