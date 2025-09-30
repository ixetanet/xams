using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace Xams.Core.Entities;

[Table(nameof(AuditHistoryDetail))]
public class AuditHistoryDetail
{
    public Guid AuditHistoryDetailId { get; set; }
    [UIName]
    [UIDisplayName("Field Name")]
    [MaxLength(250)]
    public string? FieldName { get; set; }
    public Guid AuditHistoryId { get; set; }
    public AuditHistory? AuditHistory { get; set; }
    [UIDisplayName("Table Name")]
    [MaxLength(250)]
    public string? TableName { get; set; }
    [UIDisplayName("Field Type")]
    [MaxLength(30)]
    public string? FieldType { get; set; }
    public Guid? OldValueId { get; set; }
    [UIDisplayName("Old Value")]
    [MaxLength(8000)]
    public string? OldValue { get; set; }
    public Guid? NewValueId { get; set; }
    [UIDisplayName("New Value")]
    [MaxLength(8000)]
    public string? NewValue { get; set; }
}