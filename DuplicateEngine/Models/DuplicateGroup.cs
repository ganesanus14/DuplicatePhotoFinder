namespace DuplicateEngine.Models;

public class DuplicateGroup
{
    public int GroupId { get; set; }
    public List<PhotoFile> Photos { get; set; } = new();
    public int Count => Photos.Count;
    public double Similarity { get; set; }
}
