namespace Xams.Core.Attributes;

public class JobTimeZone : Attribute
{
    public string TimeZone { get; private set; }
    public JobTimeZone(string timeZone)
    {
        TimeZone = timeZone;
    }
}