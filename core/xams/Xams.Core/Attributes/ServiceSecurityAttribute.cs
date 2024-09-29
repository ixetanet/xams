namespace Xams.Core.Attributes;

public class ServiceSecurityAttribute : Attribute
{
    public int Order { get; set; }
    public ServiceSecurityAttribute(int order = 100)
    {
        Order = order;
    }
}