using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace Example.Common.Entities;

[Table("Job")]
public class Job
{
    public Guid JobId { get; set; }
    [UIReadOnly]
    public string Name { get; set; }
    [UIDisplayName("Active")]
    public bool IsActive { get; set; }
    [UIReadOnly]
    public string Queue { get; set; }
    [UIReadOnly]
    public string Status { get; set; }
    [UIReadOnly]
    [UIDisplayName("Last Execution")]
    public DateTime LastExecution { get; set; }
    [UIReadOnly]
    public DateTime Ping { get; set; }
    
    public ICollection<JobHistory> JobHistories { get; set; } = null!;
}