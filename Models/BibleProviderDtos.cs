namespace BibleReader.Api.Services.Models;

public sealed class BibleVersionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Language { get; set; }
}

public sealed class BibleBookDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public int? ChapterCount { get; set; }
}

public sealed class BibleVerseDto
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
}

public sealed class BibleChapterDto
{
    public string BookExternalId { get; set; } = string.Empty;
    public int ChapterNumber { get; set; }
    public List<BibleVerseDto> Verses { get; set; } = new();
}