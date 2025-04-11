namespace Xams.Core.Attributes;

public class UIDisplayNameUpdatedDateAttribute : Attribute
{
    public string DisplayName { get; set; }
    public UIDisplayNameUpdatedDateAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}