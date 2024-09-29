namespace Xams.Core.Attributes;

[Flags]
public enum StartupOperation
{
    Pre = 1,
    Post = 2
}

public class ServiceStartupAttribute : Attribute
{
    public StartupOperation StartupOperation { get; private set; }
    public int Order { get; private set; }
    public ServiceStartupAttribute(StartupOperation startupOperation, int order = 0)
    {
        StartupOperation = startupOperation;
        Order = order;
    }
}