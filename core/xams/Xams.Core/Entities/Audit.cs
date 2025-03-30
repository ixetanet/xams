using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace Xams.Core.Entities;

[Table(nameof(Audit))]
public class Audit
{
    public Guid AuditId { get; set; }
    [MaxLength(250)]
    [UIReadOnly]
    [UIDisplayName("Table Name")]
    public string? Name { get; set; } 
    [UIDisplayName("Create")]
    public bool IsCreate { get; set; }
    [UIDisplayName("Read")]
    public bool IsRead { get; set; }
    [UIDisplayName("Update")]
    public bool IsUpdate { get; set; }
    [UIDisplayName("Delete")]
    public bool IsDelete { get; set; }
    public bool IsTable { get; set; }
    public ICollection<AuditField> AuditFields { get; set; } = null!;
}