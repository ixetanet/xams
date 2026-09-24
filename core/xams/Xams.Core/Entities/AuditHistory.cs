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
    /// <summary>
    /// The user's name when the change was made. It is kept when the user is later deleted
    /// and UserId is cleared.
    /// </summary>
    [UIDisplayName("User Name")]
    [UIReadOnly]
    [MaxLength(250)]
    public string? UserName { get; set; }
    [UIReadOnly]
    [UIDateFormat("lll")]
    public DateTime CreatedDate { get; set; }
    [UIReadOnly]
    public Guid TransactionId { get; set; }
}
