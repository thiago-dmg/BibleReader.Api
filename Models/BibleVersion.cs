namespace BibleReader.Api.Models;

public class BibleVersion
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<BibleBook> Books { get; set; } = new List<BibleBook>();
}
