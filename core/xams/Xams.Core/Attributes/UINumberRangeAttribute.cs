namespace Xams.Core.Attributes;

public class UINumberRangeAttribute : Attribute
{
    public float Min { get; set; }
    public float Max { get; set; }
    
    public UINumberRangeAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}