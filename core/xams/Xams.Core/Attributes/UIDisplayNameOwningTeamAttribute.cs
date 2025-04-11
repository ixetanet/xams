namespace Xams.Core.Attributes;

public class UIDisplayNameOwningTeamAttribute : Attribute
{
    public string DisplayName { get; set; }
    public UIDisplayNameOwningTeamAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}