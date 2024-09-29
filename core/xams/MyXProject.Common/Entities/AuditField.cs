using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace MyXProject.Common.Entities;

[Table(nameof(AuditField))]
public class AuditField
{
    public Guid AuditFieldId { get; set; }
    [UIDisplayName("Field Name")]
    [MaxLength(250)]
    public string? Name { get; set; }
    public Guid AuditId { get; set; }
    public Audit? Audit { get; set; }
    [UIDisplayName("Create")]
    public bool IsCreate { get; set; }
    [UIDisplayName("Update")]
    public bool IsUpdate { get; set; }
    [UIDisplayName("Delete")]
    public bool IsDelete { get; set; }
}