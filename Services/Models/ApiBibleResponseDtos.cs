namespace BibleReader.Api.Services.Models;

public class ApiBibleListResponse<T>
{
    public List<T>? Data { get; set; }
}

public class ApiBibleObjectResponse<T>
{
    public T? Data { get; set; }
}

public class ApiBibleVersionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? NameLocal { get; set; }
    public ApiBibleLanguageItem? Language { get; set; }
}

public class ApiBibleLanguageItem
{
    public string? Name { get; set; }
    public string? NameLocal { get; set; }
}

public class ApiBibleBookItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Abbreviation { get; set; }
}

public class ApiBibleChapterItem
{
    public string? Id { get; set; }
    public string? Number { get; set; }
    public List<ApiBibleContentNode>? Content { get; set; }
}

public class ApiBibleContentNode
{
    public string? Type { get; set; }
    public string? Number { get; set; }
    public string? Text { get; set; }
    public List<ApiBibleContentNode>? Items { get; set; }
}