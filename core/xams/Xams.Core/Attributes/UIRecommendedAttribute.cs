// ReSharper disable InconsistentNaming
namespace Xams.Core.Attributes;

public class UIRecommendedAttribute : Attribute
{
    public string[] Fields { get; private set; }
    public UIRecommendedAttribute()
    {
        Fields = [];
    }

    public UIRecommendedAttribute(params string [] fields)
    {
        Fields = fields;
    }
}