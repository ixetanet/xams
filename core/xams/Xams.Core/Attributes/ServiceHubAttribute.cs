namespace Xams.Core.Attributes;

public class ServiceHubAttribute : Attribute
{
    public string Name { get; set; }

    public ServiceHubAttribute(string name)
    {
        Name = name;
    }
}