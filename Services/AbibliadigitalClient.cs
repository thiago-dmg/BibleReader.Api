using System.Net.Http.Headers;
using System.Text.Json;
using BibleReader.Api.External;
using BibleReader.Api.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BibleReader.Api.Services;

public sealed class AbibliadigitalClientOptions
{
    public const string SectionName = "ExternalBible";

    public string BaseUrl { get; set; } = "https://www.abibliadigital.com.br/api/";
    public bool Enabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 45;
    /// <summary>Token opcional da A Bíblia Digital (Authorization: Bearer) para limites maiores.</summary>
    public string? BearerToken { get; set; }
    public string UserAgent { get; set; } = "BibleReader.Api.Vps/1.0 (+https://github.com/thiago-dmg/BibleReader.Api.Vps)";
}

public sealed class AbibliadigitalClient : IAbibliadigitalClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AbibliadigitalClient> _logger;
    private readonly AbibliadigitalClientOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AbibliadigitalClient(
        HttpClient http,
        IOptions<AbibliadigitalClientOptions> options,
        ILogger<AbibliadigitalClient> logger)
    {
        _http = http;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<AbibliadigitalChapterPayload?> GetChapterAsync(
        string versionLower,
        string bookAbbrev,
        int chapterNumber,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return null;

        var path =
            $"verses/{Uri.EscapeDataString(versionLower)}/{Uri.EscapeDataString(bookAbbrev)}/{chapterNumber}";
        try
        {
            using var response = await _http.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "A Bíblia Digital retornou {Status} para {Path}. Corpo: {Body}",
                    (int)response.StatusCode,
                    path,
                    body.Length > 500 ? body[..500] + "…" : body);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<AbibliadigitalChapterPayload>(stream, JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao buscar capítulo na A Bíblia Digital: {Path}", path);
            return null;
        }
    }
}

public static class AbibliadigitalHttpClientExtensions
{
    public static IServiceCollection AddAbibliadigitalClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AbibliadigitalClientOptions>(
            configuration.GetSection(AbibliadigitalClientOptions.SectionName));

        services.AddHttpClient<IAbibliadigitalClient, AbibliadigitalClient>((sp, client) =>
        {
            var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AbibliadigitalClientOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(opt.BaseUrl)
                ? "https://www.abibliadigital.com.br/api/"
                : opt.BaseUrl.Trim();
            if (!baseUrl.EndsWith('/'))
                baseUrl += "/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds <= 0 ? 45 : opt.TimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(opt.UserAgent))
                client.DefaultRequestHeaders.UserAgent.ParseAdd(opt.UserAgent);
            if (!string.IsNullOrWhiteSpace(opt.BearerToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opt.BearerToken.Trim());
        });

        return services;
    }
}
