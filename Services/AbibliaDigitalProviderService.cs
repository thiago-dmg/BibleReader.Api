using BibleReader.Api.Services.Models;

namespace BibleReader.Api.Services;

public sealed class AbibliaDigitalProviderService : IBibleProviderService
{
    private readonly HttpClient _httpClient;

    public AbibliaDigitalProviderService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<BibleVersionDto>> GetVersionsAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return new List<BibleVersionDto>
        {
            new() { Code = "acf", Name = "Almeida Corrigida Fiel", Language = "pt-BR" },
            new() { Code = "nvi", Name = "Nova Versão Internacional", Language = "pt-BR" }
        };
    }

    public async Task<IEnumerable<BibleBookDto>> GetBooksAsync(
        string versionCode,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return new List<BibleBookDto>();
    }

    public async Task<BibleChapterDto> GetChapterAsync(
        string versionCode,
        string bookExternalId,
        int chapterNumber,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return new BibleChapterDto
        {
            BookExternalId = bookExternalId,
            ChapterNumber = chapterNumber,
            Verses = new List<BibleVerseDto>()
        };
    }

    public async Task<BibleVerseDto?> GetVerseAsync(
        string versionCode,
        string bookExternalId,
        int chapterNumber,
        int verseNumber,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return null;
    }
}