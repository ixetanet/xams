namespace Xams.Core.Attributes;

public class UIDateFormatAttribute : Attribute
{
    private static readonly string[] _timeParts =
    [
        "h",
        "m",
        "s",
        "A",
        "a",
        "H",
        "k",
        "K",
        "m",
        "s",
        "S",
        "Z",
        "X",
        "LT",
        "LTS",
        "LLL",
        "LLLL",
        "lll",
        "llll",
    ];

    public string DateFormat { get; set; }

    public UIDateFormatAttribute(string dateFormat)
    {
        DateFormat = dateFormat;
    }
    
    public bool HasTimePart()
    {
        return _timeParts.Any(x => DateFormat.Contains(x));
    }
}