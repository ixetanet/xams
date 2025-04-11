
namespace Xams.Core.Jobs;

public class JobOptions
{
    public required string JobName { get; set; }
    
    /// <summary>
    /// Optionally specify Job Server
    /// </summary>
    public string? JobServer { get; set; }
    
    /// <summary>
    /// Any class or anonymous type 
    /// </summary>
    public object? Parameters { get; set; }
    
    /// <summary>
    /// Force set the JobHistoryId. Useful for monitoring a job status.
    /// </summary>
    public Guid? JobHistoryId { get; set; }
}