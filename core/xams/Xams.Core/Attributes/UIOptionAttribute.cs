namespace Xams.Core.Attributes;

public class UIOptionAttribute : Attribute
{
    public string Name { get; set; }
    public UIOptionAttribute(string name)
    {
        this.Name = name;
    }
}