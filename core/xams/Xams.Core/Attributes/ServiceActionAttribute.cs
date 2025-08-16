namespace Xams.Core.Attributes;

public class ServiceActionAttribute : Attribute
{
    public string Name { get; set; }

    public ServiceActionAttribute(string name)
    {
        Name = name;
    }
}