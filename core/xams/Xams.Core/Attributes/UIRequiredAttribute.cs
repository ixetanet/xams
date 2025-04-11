// ReSharper disable InconsistentNaming
namespace Xams.Core.Attributes;

public class UIRequiredAttribute : Attribute
{
    public string[] Fields { get; private set; }
    public UIRequiredAttribute()
    {
        Fields = [];
    }

    public UIRequiredAttribute(params string[] fields)
    {
        Fields = fields;
    }
}