namespace Xams.Core.Attributes;

public enum ExecuteJobOn
{
    All,
    One
}

public class JobServerAttribute(ExecuteJobOn executeJobOn, string? serverName = null) : Attribute
{
    public ExecuteJobOn ExecuteJobOn { get; } = executeJobOn;
    public string? ServerName { get; } = serverName;
}