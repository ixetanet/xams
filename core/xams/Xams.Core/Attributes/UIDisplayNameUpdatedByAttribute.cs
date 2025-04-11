namespace Xams.Core.Attributes;

public class UIDisplayNameUpdatedByAttribute : Attribute
{
    public string DisplayName { get; set; }
    public UIDisplayNameUpdatedByAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}