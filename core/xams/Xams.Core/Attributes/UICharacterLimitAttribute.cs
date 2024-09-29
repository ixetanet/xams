namespace Xams.Core.Attributes;

public class UICharacterLimitAttribute : Attribute
{
    public int Limit { get; set; }
    public UICharacterLimitAttribute(int limit)
    {
        this.Limit = limit;
    }
}