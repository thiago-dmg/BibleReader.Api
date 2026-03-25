using System.Net.Http.Headers;
using System.Text.Json;
using BibleReader.Api.Services.Models;
using Microsoft.Extensions.Options;

namespace BibleReader.Api.Services;

public class ApiBibleProviderService : IBibleProviderService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalBibleOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiBibleProviderService(
        HttpClient httpClient,
        IOptions<ExternalBibleOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IEnumerable<BibleVersionDto>> GetVersionsAsync(CancellationToken cancellationToken = default)
    {
        ValidateConfig();

        using var request = CreateRequest(HttpMethod.Get, "bibles");
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        await EnsureSuccessAsync(response);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<ApiBibleListResponse<ApiBibleVersionItem>>(
            stream,
            JsonOptions,
            cancellationToken);

        return payload?.Data?.Select(x => new BibleVersionDto
        {
            Code = x.Id ?? string.Empty,
            Name = x.NameLocal ?? x.Name ?? string.Empty,
            Language = x.Language?.NameLocal ?? x.Language?.Name ?? string.Empty
        }) ?? Enumerable.Empty<BibleVersionDto>();
    }

    public async Task<IEnumerable<BibleBookDto>> GetBooksAsync(
        string versionCode,
        CancellationToken cancellationToken = default)
    {
        ValidateConfig();

        versionCode = ResolveVersionCode(versionCode);

        using var request = CreateRequest(HttpMethod.Get, $"bibles/{versionCode}/books");
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        await EnsureSuccessAsync(response);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<ApiBibleListResponse<ApiBibleBookItem>>(
            stream,
            JsonOptions,
            cancellationToken);

        return payload?.Data?.Select(x => new BibleBookDto
        {
            ExternalId = x.Id ?? string.Empty,
            Name = x.Name ?? string.Empty,
            Abbreviation = x.Abbreviation ?? string.Empty,
            ChapterCount = null
        }) ?? Enumerable.Empty<BibleBookDto>();
    }

    public async Task<BibleChapterDto> GetChapterAsync(
        string versionCode,
        string bookExternalId,
        int chapterNumber,
        CancellationToken cancellationToken = default)
    {
        ValidateConfig();

        versionCode = ResolveVersionCode(versionCode);

        if (string.IsNullOrWhiteSpace(bookExternalId))
            throw new InvalidOperationException("bookExternalId é obrigatório.");

        var chapterId = $"{bookExternalId}.{chapterNumber}";

        using var request = CreateRequest(
            HttpMethod.Get,
            $"bibles/{versionCode}/chapters/{chapterId}/verses");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        await EnsureSuccessAsync(response);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<ApiBibleListResponse<ApiBibleVerseItem>>(
            stream,
            JsonOptions,
            cancellationToken);

        var verses = payload?.Data?
            .Select(v => new BibleVerseDto
            {
                Number = ParseVerseNumber(v.Number, v.Id),
                Text = v.Text ?? string.Empty
            })
            .Where(v => v.Number > 0 && !string.IsNullOrWhiteSpace(v.Text))
            .ToList() ?? new List<BibleVerseDto>();

        return new BibleChapterDto
        {
            BookExternalId = bookExternalId,
            ChapterNumber = chapterNumber,
            Verses = verses
        };
    }

    public async Task<BibleVerseDto?> GetVerseAsync(
        string versionCode,
        string bookExternalId,
        int chapterNumber,
        int verseNumber,
        CancellationToken cancellationToken = default)
    {
        ValidateConfig();

        versionCode = ResolveVersionCode(versionCode);

        if (string.IsNullOrWhiteSpace(bookExternalId))
            throw new InvalidOperationException("bookExternalId é obrigatório.");

        var verseId = $"{bookExternalId}.{chapterNumber}.{verseNumber}";

        using var request = CreateRequest(
            HttpMethod.Get,
            $"bibles/{versionCode}/verses/{verseId}");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<ApiBibleObjectResponse<ApiBibleVerseItem>>(
            stream,
            JsonOptions,
            cancellationToken);

        if (payload?.Data is null)
            return null;

        return new BibleVerseDto
        {
            Number = ParseVerseNumber(payload.Data.Number, payload.Data.Id),
            Text = (payload.Data.Content ?? payload.Data.Text ?? string.Empty).Trim()
        };
    }

    private void ValidateConfig()
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("ExternalBible está desabilitado.");

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException("ExternalBible:BaseUrl não configurado.");

        if (string.IsNullOrWhiteSpace(_options.ApiKey) && string.IsNullOrWhiteSpace(_options.BearerToken))
            throw new InvalidOperationException("ExternalBible:ApiKey ou ExternalBible:BearerToken deve ser informado.");
    }

    private string ResolveVersionCode(string? versionCode)
    {
        var resolved = string.IsNullOrWhiteSpace(versionCode)
            ? _options.DefaultVersionCode
            : versionCode;

        if (string.IsNullOrWhiteSpace(resolved))
            throw new InvalidOperationException("ExternalBible:DefaultVersionCode não configurado e versionCode não informado.");

        return resolved;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/') + "/";
        var url = baseUrl + relativeUrl.TrimStart('/');

        var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            request.Headers.Add("api-key", _options.ApiKey);

        if (!string.IsNullOrWhiteSpace(_options.BearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.BearerToken);

        if (!string.IsNullOrWhiteSpace(_options.UserAgent))
            request.Headers.UserAgent.ParseAdd(_options.UserAgent);

        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();

        throw new HttpRequestException(
            $"Falha ao chamar API da Bíblia. Status: {(int)response.StatusCode}. Body: {body}");
    }

    private static int ParseVerseNumber(string? number, string? id)
    {
        if (int.TryParse(number, out var parsed))
            return parsed;

        if (!string.IsNullOrWhiteSpace(id))
        {
            var last = id.Split('.').LastOrDefault();
            if (int.TryParse(last, out parsed))
                return parsed;
        }

        return 0;
    }
}