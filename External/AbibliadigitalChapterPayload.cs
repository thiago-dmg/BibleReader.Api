using System.Text.Json.Serialization;

namespace BibleReader.Api.External;

public sealed class AbibliadigitalChapterPayload
{
    [JsonPropertyName("verses")]
    public List<AbibliadigitalVerseItem>? Verses { get; set; }
}

public sealed class AbibliadigitalVerseItem
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}
