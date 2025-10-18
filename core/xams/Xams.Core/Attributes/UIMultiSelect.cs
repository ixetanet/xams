namespace Xams.Core.Attributes;

public class UIMultiSelectAttribute : Attribute
{
    public string JunctionOwnerIdField { get; set; }
    public string JunctionTargetIdField { get; set; }

    public UIMultiSelectAttribute(string junctionOwnerIdField, string junctionTargetIdField)
    {
        JunctionOwnerIdField = junctionOwnerIdField;
        JunctionTargetIdField = junctionTargetIdField;
    }
}