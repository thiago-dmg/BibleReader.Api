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

public class ApiBibleVerseRefItem
{
    public string? Id { get; set; }
    public string? OrgId { get; set; }
    public string? BookId { get; set; }
    public string? ChapterId { get; set; }
    public string? BibleId { get; set; }
}

public class ApiBibleVerseItem
{
    public string? Id { get; set; }
    public string? Number { get; set; }
    public string? Text { get; set; }
    public string? Content { get; set; }
}