using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace MyXProject.Common.Entities;

[Table("Job")]
public class Job
{
    public Guid JobId { get; set; }
    
    [UIReadOnly]
    [MaxLength(250)]
    public string? Name { get; set; }
    
    [UIDisplayName("Active")]
    public bool IsActive { get; set; }
    
    [UIReadOnly]
    [MaxLength(100)]
    public string? Queue { get; set; }
    
    [UIReadOnly]
    [UIDisplayName("Last Execution")]
    [UIDateFormat("lll")]
    public DateTime LastExecution { get; set; }
    
    [UIHide(true)]
    [MaxLength(100)]
    public string? Tag { get; set; }
    
    public ICollection<JobHistory> JobHistories { get; set; } = null!;
}