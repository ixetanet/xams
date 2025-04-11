namespace Xams.Core.Attributes;

public class UIDisplayNameCreatedByAttribute : Attribute
{
    public string DisplayName { get; set; }
    public UIDisplayNameCreatedByAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}