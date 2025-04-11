namespace Xams.Core.Attributes;

public class UIDisplayNameCreatedDateAttribute : Attribute
{
    public string DisplayName { get; set; }
    public UIDisplayNameCreatedDateAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}