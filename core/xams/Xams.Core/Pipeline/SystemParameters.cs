namespace Xams.Core.Pipeline;

public struct SystemParameters
{
    public bool ReturnEmpty { get; set; } // True if Create \ Update \ Upsert should return empty response and not retrieve entity
    public bool ReturnEntity { get; set; } // True if Create \ Update \ Upsert should return c# class entity vs dynamic object
    public bool NoPostOrderTraversalDelete { get; set; } // True to prevent post order traversal delete
    public bool PreventSave { get; set; } // True to prevent save
}