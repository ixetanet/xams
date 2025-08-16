namespace Xams.Core.Dtos.Data;

public class ReadOutput
{
    public int pages { get; set; }
    public int currentPage { get; set; }
    public int totalResults { get; set; }
    public int maxResults { get; set; }
    public string tableName { get; set; }
    public OrderBy[]? orderBy { get; set; }
    public bool? distinct { get; set; }
    public bool? denormalize { get; set; }
    // public string? order { get; set; }
    // public string? orderBy { get; set; }
    public List<object> results { get; set; }
    public dynamic parameters { get; set; } = null!;
}