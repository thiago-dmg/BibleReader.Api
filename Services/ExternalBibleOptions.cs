namespace BibleReader.Api.Services;

public class ExternalBibleOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int TimeoutSeconds { get; set; } = 45;
    public string ApiKey { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
    public string DefaultVersionCode { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "BibleReader.Api";
}