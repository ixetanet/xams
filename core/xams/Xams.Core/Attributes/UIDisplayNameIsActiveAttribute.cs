namespace Xams.Core.Attributes;

public class UIDisplayNameIsActiveAttribute : Attribute
{
    public string DisplayName { get; set; }
    public UIDisplayNameIsActiveAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}