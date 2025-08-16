// ReSharper disable InconsistentNaming
namespace Xams.Core.Attributes;

public class UIReadOnlyAttribute : Attribute
{
    public string[] Fields { get; private set; }
    public UIReadOnlyAttribute()
    {
        Fields = [];
    }

    public UIReadOnlyAttribute(params string[] fields)
    {
        Fields = fields;
    }
}