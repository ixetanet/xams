namespace Xams.Core.Attributes;

public enum BulkStage
{
    Pre,
    Post
}
public class BulkServiceAttribute : Attribute
{
    public int Order { get; set; }
    public BulkStage Stage { get;  set; }
    
    public BulkServiceAttribute(BulkStage bulkStage, int order = 0)
    {
        Stage = bulkStage;
        Order = order;
    }
}