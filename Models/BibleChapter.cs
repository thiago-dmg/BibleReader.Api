namespace BibleReader.Api.Models;

public class BibleChapter
{
    public int Id { get; set; }
    public int BibleBookId { get; set; }
    public int ChapterNumber { get; set; }
    /// <summary>Ordem global na Bíblia (1..N), usada para distribuir o plano.</summary>
    public int GlobalOrder { get; set; }

    public BibleBook BibleBook { get; set; } = null!;
}
