using BibleReader.Api.External;

namespace BibleReader.Api.Interfaces;

public interface IAbibliadigitalClient
{
    /// <summary>
    /// GET /api/verses/{version}/{abbrev}/{chapter}
    /// </summary>
    Task<AbibliadigitalChapterPayload?> GetChapterAsync(
        string versionLower,
        string bookAbbrev,
        int chapterNumber,
        CancellationToken cancellationToken = default);
}
