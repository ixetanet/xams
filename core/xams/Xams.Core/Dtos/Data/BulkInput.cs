namespace Xams.Core.Dtos.Data;

public class BulkInput
{
    public Input[]? Creates { get; set; }
    public Input[]? Updates { get; set; }
    public Input[]? Deletes { get; set; }
    public Input[]? Upserts { get; set; }
}