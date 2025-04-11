namespace Xams.Core.Attributes;

public class UIDisplayNameOwningUserAttribute : Attribute
{
    public string DisplayName { get; set; }
    public UIDisplayNameOwningUserAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}