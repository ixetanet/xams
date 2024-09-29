using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace Example.Common.Entities;

[Table("JobHistory")]
public class JobHistory 
{
    public Guid JobHistoryId { get; set; }
    
    [UIReadOnly]
    [UISetFieldFromLookup(nameof(JobId))]
    public string Name { get; set; }
    [UIReadOnly]
    public Guid JobId { get; set; }
    public Job Job { get; set; }
    
    [UIReadOnly]
    public string Status { get; set; }
    [UIReadOnly]
    public string Message { get; set; }
    
    [UIReadOnly]
    [UIDateFormat("lll")]
    public DateTime CreatedDate { get; set; }
    
    [UIReadOnly]
    [UIDateFormat("lll")]
    public DateTime? CompletedDate { get; set; }
}