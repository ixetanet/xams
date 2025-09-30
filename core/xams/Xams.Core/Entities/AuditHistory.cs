using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace Xams.Core.Entities;

[Table(nameof(AuditHistory))]
public class AuditHistory
{
    public Guid AuditHistoryId { get; set; }
    [UIDisplayName("Record Name")]
    [UIReadOnly]
    [MaxLength(250)]
    public required string Name { get; set; }
    [UIDisplayName("Table Name")]
    [UIReadOnly]
    [MaxLength(250)]
    public string? TableName { get; set; }
    [UIReadOnly]
    [MaxLength(250)]
    public string? EntityId { get; set; }
    [UIReadOnly]
    [MaxLength(10)]
    public required string Operation { get; set; }
    [UIReadOnly]
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    [UIReadOnly]
    [UIDateFormat("lll")]
    public DateTime CreatedDate { get; set; }
    [UIReadOnly]
    [MaxLength(Int32.MaxValue)]
    public string? Query { get; set; }
    [UIReadOnly]
    [MaxLength(Int32.MaxValue)]
    public string? Results { get; set; }
}