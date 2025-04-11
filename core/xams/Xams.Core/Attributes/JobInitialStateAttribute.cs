namespace Xams.Core.Attributes;

public enum JobState
{
    Active,
    Inactive,
}

public class JobInitialStateAttribute : Attribute
{
    public JobState State { get; }
    public JobInitialStateAttribute(JobState state)
    {
        State = state;
    }   
}