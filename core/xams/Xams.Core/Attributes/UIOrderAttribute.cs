namespace Xams.Core.Attributes;

public class UIOrderAttribute : Attribute
{
    public int Order { get; set; }
    public UIOrderAttribute(int order)
    {
        this.Order = order;
    }
}