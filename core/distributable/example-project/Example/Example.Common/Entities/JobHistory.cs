using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace Example.Common.Entities;

[Table(nameof(JobHistory))]
public class JobHistory 
{
    public Guid JobHistoryId { get; set; }
    
    [UIReadOnly]
    [UISetFieldFromLookup(nameof(JobId))]
    [MaxLength(250)]
    public string? Name { get; set; } 

    [UIReadOnly]
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;

    [UIReadOnly]
    [MaxLength(250)]
    public string? Status { get; set; }

    [UIReadOnly]
    [MaxLength(2000)]
    public string? Message { get; set; }

    [UIReadOnly]
    [UIDateFormat("lll")]
    public DateTime CreatedDate { get; set; }
    
    [UIReadOnly]
    [UIDateFormat("lll")]
    public DateTime? CompletedDate { get; set; }
}