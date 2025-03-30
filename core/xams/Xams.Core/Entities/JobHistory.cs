using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;

namespace Xams.Core.Entities;

[Table(nameof(JobHistory))]
[Index(nameof(ServerName), nameof(Ping), IsDescending = [false, true], Name = "IX_JobHistory_ServerName_Ping")]
public class JobHistory 
{
    public Guid JobHistoryId { get; set; }
    
    [UIReadOnly]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [UIReadOnly]
    public Guid JobId { get; set; }
    public Job? Job { get; set; }
    
    [UIReadOnly]
    [MaxLength(20)]
    public string Status { get; set; } = null!;

    [UIReadOnly]
    [MaxLength(4000)]
    public string Message { get; set; } = null!;

    [UIReadOnly]
    [UIDateFormat("lll")]
    public DateTime CreatedDate { get; set; }
    
    [UIReadOnly]
    [UIDateFormat("lll")]
    public DateTime? CompletedDate { get; set; }
    
    [UIReadOnly]
    [UIDateFormat("lll")]
    public DateTime Ping { get; set; }
    
    [UIReadOnly]
    [MaxLength(100)]
    public string ServerName { get; set; } = null!;
}