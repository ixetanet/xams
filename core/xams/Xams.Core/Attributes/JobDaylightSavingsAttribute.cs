namespace Xams.Core.Attributes;

public class JobDaylightSavingsAttribute : Attribute
{
    public string TimeZone { get; private set; }
    public JobDaylightSavingsAttribute(string timeZone)
    {
        TimeZone = timeZone;
    }
}