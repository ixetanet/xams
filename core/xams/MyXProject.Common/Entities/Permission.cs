using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyXProject.Common.Entities;

[Table(nameof(Permission))]
public class Permission
{
    public Guid PermissionId { get; set; }
    [MaxLength(300)]
    public string? Name { get; set; }
    [MaxLength(250)]
    public string? Tag { get; set; }
}