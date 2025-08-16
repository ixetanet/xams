using System.Text.Json;

namespace Xams.Core.Dtos.Data;

public class ReadInput
{
    public string tableName { get; set; }
    public string[] fields { get; set; } = null!;
    public object? id { get; set; }
    public OrderBy[]? orderBy { get; set; }
    public int? maxResults { get; set; }
    public int page { get; set; }
    public bool? distinct { get; set; }
    public bool? denormalize { get; set; }
    public Filter[]? filters { get; set; }
    public Join[]? joins { get; set; }
    public Exclude[]? except { get; set; }
    public Dictionary<string, JsonElement>? parameters { get; set; }

    public object GetId()
    {
        if (id == null)
        {
            throw new Exception("No id in read request");
        }
                
        if (((JsonElement)id).ValueKind == JsonValueKind.Number)
        {
            return ((JsonElement)id).GetInt64();
        }

        if (Guid.TryParse(((JsonElement)id).GetString(), out var guid))
        {
            return guid;
        }
        throw new Exception($"Unrecognized id in read");
    }
    
}

public class Join
{
    public string? type { get; set; }
    public string[] fields { get; set; } = null!;
    public string? alias { get; set; }
    public string fromTable { get; set; } = null!;
    public string fromField { get; set; } = null!;
    public string toTable { get; set; } = null!;
    public string toField { get; set; } = null!;
    public Filter[]? filters { get; set; }
}

public class Filter
{
    public string? logicalOperator { get; set; }
    public Filter[]? filters { get; set; }
    public string? field { get; set; }
    public string? @operator { get; set; }
    public string? value { get; set; }

}
    
public class OrderBy
{
    public string field { get; set; }
    public string? order { get; set; }
}

public class Exclude
{
    public string fromField { get; set; }
    public ReadInput query { get; set; }
}