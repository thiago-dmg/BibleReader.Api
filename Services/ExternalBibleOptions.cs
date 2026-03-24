namespace BibleReader.Api.Services;

public class ExternalBibleOptions
{
    public string Provider { get; set; } = "ApiBible";
    public string BaseUrl { get; set; } = "";
    public bool Enabled { get; set; }
    public int TimeoutSeconds { get; set; } = 45;
    public string ApiKey { get; set; } = "";
    public string UserAgent { get; set; } = "";
}