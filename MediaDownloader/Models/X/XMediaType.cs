namespace MediaDownloader.Models.X;

[Flags]
public enum XMediaType
{
    Image = 0b001,
    Video = 0b010,
    Gif = 0b100,
    All = 0b111,
}
