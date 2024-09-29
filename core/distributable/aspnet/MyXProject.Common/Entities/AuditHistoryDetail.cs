using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace MyXProject.Common.Entities;

[Table(nameof(AuditHistoryDetail))]
public class AuditHistoryDetail
{
    public Guid AuditHistoryDetailId { get; set; }
    [UIDisplayName("Field Name")]
    [MaxLength(250)]
    public string? Name { get; set; }
    public Guid AuditHistoryId { get; set; }
    public AuditHistory? AuditHistory { get; set; }
    [MaxLength(250)]
    public string? EntityName { get; set; }
    [MaxLength(30)]
    public string? FieldType { get; set; }
    public Guid? OldValueId { get; set; }
    [MaxLength(8000)]
    public string? OldValue { get; set; }
    public Guid? NewValueId { get; set; }
    [MaxLength(8000)]
    public string? NewValue { get; set; }
}