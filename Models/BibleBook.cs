namespace BibleReader.Api.Models;

public class BibleBook
{
    public int Id { get; set; }
    public int BibleVersionId { get; set; }
    public int Order { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public int ChapterCount { get; set; }

    public BibleVersion BibleVersion { get; set; } = null!;
    public ICollection<BibleChapter> Chapters { get; set; } = new List<BibleChapter>();
}
