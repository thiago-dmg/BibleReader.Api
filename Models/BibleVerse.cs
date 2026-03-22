namespace BibleReader.Api.Models;

public class BibleVerse
{
    public int Id { get; set; }
    public int BibleChapterId { get; set; }
    public int VerseNumber { get; set; }
    public string Text { get; set; } = string.Empty;

    public BibleChapter BibleChapter { get; set; } = null!;
}
