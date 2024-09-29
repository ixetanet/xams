namespace Xams.Core.Attributes;

public class UIDateFormatAttribute : Attribute
{
    public string DateFormat { get; set; }
    public UIDateFormatAttribute(string dateFormat)
    {
        DateFormat = dateFormat;
    }
}